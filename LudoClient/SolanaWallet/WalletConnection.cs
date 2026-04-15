using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Wallet;

namespace LudoClient.SolanaWallet
{
    public class WalletConnection
    {
        private MobileWalletConnection _connection = new();
        private IRpcClient _rpcClient = ClientFactory.GetClient(Cluster.MainNet);
        private string _clusterName = "mainnet-beta";

        public MobileWalletAdapterClient? Client => _connection.Client;
        public bool LastLaunchCanceled => _connection.LastLaunchCanceled;
        public event Action? RemoteClosed
        {
            add => _connection.RemoteClosed += value;
            remove => _connection.RemoteClosed -= value;
        }

        public string? AuthToken 
        { 
            get => Preferences.Get("WalletAuthToken", "");
            private set => Preferences.Set("WalletAuthToken", value ?? "");
        }

        private string? CachedAccountsJson
        {
            get => Preferences.Get("WalletAuthorizedAccounts", "");
            set => Preferences.Set("WalletAuthorizedAccounts", value ?? "");
        }

        public List<AccountDetails> Accounts { get; private set; } = new();
        public byte[]? MainAddress => Accounts.FirstOrDefault()?.PublicKey;
        public string? MainAddressBase58 => Accounts.FirstOrDefault()?.DisplayAddress;

        public double SolBalance { get; private set; }
        public List<TokenBalance> TokenBalances { get; private set; } = new();

        public void SetNetwork(bool isMainnet)
        {
            var url = isMainnet ? "https://api.mainnet-beta.solana.com" : "https://api.devnet.solana.com";
            _rpcClient = ClientFactory.GetClient(url);
            _clusterName = isMainnet ? "mainnet-beta" : "devnet";
            Console.WriteLine($"[WMA] Network switched to: {_clusterName} ({url})");
            
            AuthToken = null;
            CachedAccountsJson = null;
            Accounts = new();
            SolBalance = 0;
            TokenBalances = new();
        }

        public async Task<bool> Connect()
        {
            if (Accounts.Count == 0 && !string.IsNullOrEmpty(CachedAccountsJson))
            {
                try
                {
                    Accounts = Newtonsoft.Json.JsonConvert.DeserializeObject<List<AccountDetails>>(CachedAccountsJson) ?? new();
                    Console.WriteLine($"[WMA] Restored {Accounts.Count} accounts from cache.");
                }
                catch { }
            }
            return await _connection.Connect();
        }

        public async Task DisconnectAsync()
        {
            AuthToken = null;
            CachedAccountsJson = null;
            Accounts = new();
            await _connection.DisconnectAsync();
        }

        public async Task<AuthorizationResult?> AuthorizeOrReauthorize()
        {
            if (!await Connect()) return null;

            AuthorizationResult? result = null;
            var uriIdentity = new Uri("https://ludocities.com");
            var iconRelativeUri = new Uri("faviconhq.ico", UriKind.Relative);
            string identityName = "Ludo Cities";

            string savedToken = AuthToken ?? "";
            
            if (!string.IsNullOrEmpty(savedToken))
            {
                try
                {
                    Console.WriteLine($"[WMA] Attempting Reauthorize with token: {savedToken.Substring(0, 10)}...");
                    result = await _connection.Client!.Reauthorize(
                        uriIdentity,
                        iconRelativeUri,
                        identityName,
                        savedToken
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WMA] Reauthorize failed: {ex.Message}. Falling back to Authorize.");
                    AuthToken = null;
                    CachedAccountsJson = null;
                }
            }

            if (result == null)
            {
                Console.WriteLine("[WMA] Authorizing new session...");
                result = await _connection.Client!.Authorize(
                    uriIdentity,
                    iconRelativeUri,
                    identityName,
                    _clusterName
                );
            }

            if (result != null)
            {
                AuthToken = result.AuthToken;
                
                if (result.Accounts != null && result.Accounts.Count > 0)
                {
                    Accounts = result.Accounts;
                    CachedAccountsJson = Newtonsoft.Json.JsonConvert.SerializeObject(Accounts);
                    Console.WriteLine($"[WMA] Session updated. Main Address: {MainAddressBase58}");
                }
                else if (Accounts.Count > 0)
                {
                    Console.WriteLine("[WMA] Warning: Reauthorize response had no accounts. Using cached accounts.");
                }
                else
                {
                    Console.WriteLine("[WMA] Error: No accounts available after authorization.");
                }

                await RefreshBalances();
            }
            return result;
        }

        public async Task RefreshBalances()
        {
            if (MainAddressBase58 == null) return;

            try
            {
                Console.WriteLine($"[WMA] Refreshing balances for {MainAddressBase58}...");
                
                var balanceResult = await _rpcClient.GetBalanceAsync(MainAddressBase58);
                if (balanceResult.WasSuccessful)
                {
                    SolBalance = (double)balanceResult.Result.Value / 1_000_000_000.0;
                    Console.WriteLine($"[WMA] SOL Balance: {SolBalance}");
                }

                var newList = new List<TokenBalance>();

                // SPL Tokens
                var tokensResult = await _rpcClient.GetTokenAccountsByOwnerAsync(MainAddressBase58, tokenProgramId: Solnet.Programs.TokenProgram.ProgramIdKey.ToString());
                if (tokensResult.WasSuccessful)
                {
                    ProcessTokenAccounts(tokensResult.Result.Value, newList);
                }

                // Token-2022
                var tokens2022Result = await _rpcClient.GetTokenAccountsByOwnerAsync(MainAddressBase58, tokenProgramId: SolanaTokenService.TOKEN_2022_PROGRAM_ID.ToString());
                if (tokens2022Result.WasSuccessful)
                {
                    ProcessTokenAccounts(tokens2022Result.Result.Value, newList);
                }

                TokenBalances = newList;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WMA] Error refreshing balances: " + ex.Message);
            }
        }

        private void ProcessTokenAccounts(List<Solnet.Rpc.Models.TokenAccount> accounts, List<TokenBalance> newList)
        {
            foreach (var acc in accounts)
            {
                try
                {
                    if (acc.Account.Data.Parsed == null) continue;

                    var info = acc.Account.Data.Parsed.Info;
                    var amount = info.TokenAmount.AmountDecimal;
                    
                    if (amount > 0)
                    {
                        newList.Add(new TokenBalance
                        {
                            Mint = info.Mint,
                            Amount = amount,
                            Decimals = info.TokenAmount.Decimals,
                            Symbol = "Token"
                        });
                    }
                }
                catch { }
            }
        }

        public async Task<string> SendToken(string recipientBase58, ulong amount, string mintAddress, int decimals)
        {
            if (MainAddress == null) throw new Exception("Wallet not connected");

            var blockhashResult = await _rpcClient.GetLatestBlockHashAsync();
            if (!blockhashResult.WasSuccessful) throw new Exception("Failed to get blockhash");

            var feePayer = new PublicKey(MainAddress);
            var mint = new PublicKey(mintAddress);
            var recipient = new PublicKey(recipientBase58);

            var senderAta = SolanaTokenService.FindAssociatedTokenAddress(feePayer, mint);
            var recipientAta = SolanaTokenService.FindAssociatedTokenAddress(recipient, mint);

            var txBuilder = new TransactionBuilder()
                .SetRecentBlockHash(blockhashResult.Result.Value.Blockhash)
                .SetFeePayer(feePayer);

            var recipientAtaInfo = await _rpcClient.GetAccountInfoAsync(recipientAta.ToString());
            if (!recipientAtaInfo.WasSuccessful || recipientAtaInfo.Result.Value == null)
            {
                txBuilder.AddInstruction(SolanaTokenService.CreateAssociatedTokenAccountInstruction(feePayer, recipient, mint));
            }

            txBuilder.AddInstruction(SolanaTokenService.CreateTransferCheckedInstruction(
                senderAta, mint, recipientAta, feePayer, amount, (byte)decimals
            ));
            
            var msgBytes = txBuilder.CompileMessage();
            var txBytes = new byte[1 + 64 + msgBytes.Length];
            txBytes[0] = 1;
            Array.Copy(msgBytes, 0, txBytes, 65, msgBytes.Length);
            var auth = await AuthorizeOrReauthorize();
            if (auth == null) throw new Exception("Wallet not connected");
            var signResult = await _connection.Client!.SignTransactions(new List<byte[]> { txBytes });
            if (signResult == null || signResult.SignedPayloads.Count == 0) throw new Exception("Signature failed");

            var txSignature = await _rpcClient.SendTransactionAsync(signResult.SignedPayloadsBytes[0]);
            if (!txSignature.WasSuccessful) throw new Exception($"Broadcast failed: {txSignature.Reason}");

            return txSignature.Result;
        }

        public async Task<string> SendSol(string recipientBase58, ulong lamports)
        {
            if (MainAddress == null) throw new Exception("Wallet not connected");

            var blockhashResult = await _rpcClient.GetLatestBlockHashAsync();
            if (!blockhashResult.WasSuccessful) throw new Exception("Failed to get blockhash");

            var feePayer = new PublicKey(MainAddress);
            var txBuilder = new TransactionBuilder()
                .SetRecentBlockHash(blockhashResult.Result.Value.Blockhash)
                .SetFeePayer(feePayer)
                .AddInstruction(SystemProgram.Transfer(feePayer, new PublicKey(recipientBase58), lamports));
            
            var msgBytes = txBuilder.CompileMessage();
            var txBytes = new byte[1 + 64 + msgBytes.Length];
            txBytes[0] = 1;
            Array.Copy(msgBytes, 0, txBytes, 65, msgBytes.Length);

            var auth = await AuthorizeOrReauthorize();
            if (auth == null) throw new Exception("Wallet not connected");

            var signResult = await _connection.Client!.SignTransactions(new List<byte[]> { txBytes });
            if (signResult == null || signResult.SignedPayloads.Count == 0) throw new Exception("Signature failed");

            var txSignature = await _rpcClient.SendTransactionAsync(signResult.SignedPayloadsBytes[0]);
            if (!txSignature.WasSuccessful) throw new Exception($"Broadcast failed: {txSignature.Reason}");

            return txSignature.Result;
        }
    }
}
