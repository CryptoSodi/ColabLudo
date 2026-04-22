using CommunityToolkit.Maui.Alerts;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Wallet;
using System.Text;

namespace LudoClient.SolanaWallet
{
    public class WalletConnection
    {
        private MobileWalletConnection _connection = new();
        private IRpcClient _rpcClient = ClientFactory.GetClient(Cluster.MainNet);
        private string _clusterName = "mainnet-beta";

        public event Action RemoteClosed
        {
            add => _connection.RemoteClosed += value;
            remove => _connection.RemoteClosed -= value;
        }

        public IAdapterOperations? Client => _connection.Client;

        public void SetNetwork(bool isMainnet)
        {
            var url = isMainnet ? "https://api.mainnet-beta.solana.com" : "https://api.devnet.solana.com";
            _rpcClient = ClientFactory.GetClient(url);
            _clusterName = isMainnet ? "mainnet-beta" : "devnet";
            Console.WriteLine($"[WMA] Network switched to: {_clusterName} ({url})");
            
            AuthToken = null;
            Accounts = new();
            SolBalance = 0;
            TokenBalances = new();
        }
        
        public string? AuthToken { get; private set; }
        public List<AccountDetails> Accounts { get; private set; } = new();
        public byte[]? MainAddress => Accounts.FirstOrDefault()?.PublicKey;
        public string? MainAddressBase58 {
            get {
                var acc = Accounts.FirstOrDefault();
                if (acc == null) return null;
                if (!string.IsNullOrEmpty(acc.DisplayAddress)) return acc.DisplayAddress;
                try {
                    // Fallback: Derive Base58 from raw PublicKey bytes
                    return new Solnet.Wallet.PublicKey(acc.PublicKey).Key;
                } catch {
                    return null;
                }
            }
        }

        public double SolBalance { get; private set; }
        public List<TokenBalance> TokenBalances { get; private set; } = new();

        public async Task<bool> Connect()
        {
            return await _connection.Connect();
        }

        public async Task DisconnectAsync(bool removeAuthToken)
        {
            if (removeAuthToken)
            {
                AuthToken = null;
                Accounts = new();
            }
            await _connection.DisconnectAsync();
        }

        public async Task<AuthorizationResult?> AuthorizeOrReauthorize()
        {
            if (!await Connect()) return null;

            AuthorizationResult? result = null;
            var uriIdentity = new Uri("https://ludocities.com");
            var iconRelativeUri = new Uri("faviconhq.ico", UriKind.Relative);
            string identityName = "Ludo Cities";
            
            if (!string.IsNullOrEmpty(AuthToken))
            {
                try
                {
                    Console.WriteLine($"[WMA] Attempting Reauthorize with token: {AuthToken.Substring(0, 10)}...");
                    result = await _connection.Client!.Reauthorize(
                        uriIdentity,
                        iconRelativeUri,
                        identityName,
                        AuthToken
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WMA] Reauthorize failed: {ex.Message}. Falling back to Authorize.");
                    AuthToken = null;
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
                    Console.WriteLine($"[WMA] Session updated. Main Address: {MainAddressBase58}");
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
                    SolBalance = balanceResult.Result.Value / 1000000000.0;
                }

                var newList = new List<TokenBalance>();
                var owner = new PublicKey(MainAddressBase58);
                bool isMainnet = _clusterName == "mainnet-beta";

                async Task FetchToken(string mintAddr, string symbol)
                {
                    try {
                        var tokenProgram = symbol == "USDC"
                            ? SolanaTokenService.STANDARD_TOKEN_PROGRAM_ID
                            : SolanaTokenService.TOKEN_2022_PROGRAM_ID;
                        var ata = SolanaTokenService.FindAssociatedTokenAddress(owner, new PublicKey(mintAddr), tokenProgram);
                        Console.WriteLine($"[WMA] Fetching {symbol} balance. Owner={MainAddressBase58}, Ata={ata.Key}, Mint={mintAddr}, TokenProgram={tokenProgram.Key}");
                        var bal = await _rpcClient.GetTokenAccountBalanceAsync(ata.Key);
                        if (bal.WasSuccessful && bal.Result?.Value != null)
                        {
                            decimal parsedAmount = bal.Result.Value.AmountDecimal;
                            if (parsedAmount == 0 && !string.IsNullOrWhiteSpace(bal.Result.Value.UiAmountString))
                                decimal.TryParse(bal.Result.Value.UiAmountString, out parsedAmount);

                            Console.WriteLine($"[WMA] {symbol} balance fetched. Ata={ata.Key}, Raw={bal.Result.Value.Amount}, Ui={bal.Result.Value.UiAmountString}, Parsed={parsedAmount}");
                            newList.Add(new TokenBalance {
                                Mint = mintAddr,
                                Amount = parsedAmount,
                                Decimals = bal.Result.Value.Decimals,
                                Symbol = symbol
                            });
                        }
                        else
                        {
                            Console.WriteLine($"[WMA] {symbol} balance fetch failed. Ata={ata.Key}, Success={bal.WasSuccessful}, Reason={bal.Reason}");
                        }
                    } catch { }
                }

                await Task.WhenAll(
                    FetchToken(isMainnet ? SolanaTokenService.LUDC_MINT_MAINNET : SolanaTokenService.LUDC_MINT_DEVNET, "LUDC"),
                    FetchToken(isMainnet ? SolanaTokenService.USDC_MINT_MAINNET : SolanaTokenService.USDC_MINT_DEVNET, "USDC")
                );

                TokenBalances = newList;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WMA] Error refreshing balances: " + ex.Message);
            }
        }

        public async Task<string> SignAndSendTransaction(TransactionBuilder txBuilder)
        {
            try
            {
                byte[] msgBytes;
                try 
                {
                    msgBytes = txBuilder.CompileMessage();
                }
                catch (Exception)
                {
                    var blockhashResult = await _rpcClient.GetLatestBlockHashAsync();
                    if (!blockhashResult.WasSuccessful) throw new Exception("Failed to get blockhash");
                    txBuilder.SetRecentBlockHash(blockhashResult.Result.Value.Blockhash);
                    msgBytes = txBuilder.CompileMessage();
                }
                
                // Standard Solana Transaction Wire Format for 1 signature
                var txBytes = new byte[1 + 64 + msgBytes.Length];
                txBytes[0] = 1; 
                Array.Copy(msgBytes, 0, txBytes, 65, msgBytes.Length);

                var auth = await AuthorizeOrReauthorize();
                if (auth == null) throw new Exception("Wallet not connected");

                // Request signature via MWA
                Console.WriteLine($"[WMA] Requesting signature for {txBytes.Length} byte payload...");
                var signResult = await _connection.Client!.SignTransactions(new List<byte[]> { txBytes });
                
                if (signResult == null || signResult.SignedPayloads == null || signResult.SignedPayloads.Count == 0) 
                    throw new Exception("Signature failed or declined (no payloads returned)");

                // Broadcast the signed transaction bytes using our RPC client
                Console.WriteLine("[WMA] Broadcasting signed transaction to cluster...");
                var txSignature = await _rpcClient.SendTransactionAsync(signResult.SignedPayloadsBytes[0]);
                
                if (!txSignature.WasSuccessful) 
                {
                    throw new Exception($"Broadcast failed: {txSignature.Reason}");
                }

                return txSignature.Result;
            }
            finally
            {
                await DisconnectAsync(false);
            }
        }

        public async Task<string> SignTransaction(TransactionBuilder txBuilder)
        {
            try
            {
                byte[] msgBytes;
                try 
                {
                    msgBytes = txBuilder.CompileMessage();
                }
                catch (Exception)
                {
                    var blockhashResult = await _rpcClient.GetLatestBlockHashAsync();
                    if (!blockhashResult.WasSuccessful) throw new Exception("Failed to get blockhash");
                    txBuilder.SetRecentBlockHash(blockhashResult.Result.Value.Blockhash);
                    msgBytes = txBuilder.CompileMessage();
                }
                
                var txBytes = new byte[1 + 64 + msgBytes.Length];
                txBytes[0] = 1; 
                Array.Copy(msgBytes, 0, txBytes, 65, msgBytes.Length);

                return await SignRawTransaction(txBytes);
            }
            finally
            {
                await DisconnectAsync(false);
            }
        }

        public async Task<string> SignRawTransaction(byte[] txBytes)
        {
            try
            {
                var auth = await AuthorizeOrReauthorize();
                if (auth == null) throw new Exception("Wallet not connected");

                var signResult = await _connection.Client!.SignTransactions(new List<byte[]> { txBytes });
                if (signResult == null || signResult.SignedPayloads == null || signResult.SignedPayloads.Count == 0) 
                    throw new Exception("Signature failed or declined");

                return signResult.SignedPayloads[0];
            }
            finally
            {
                await DisconnectAsync(false);
            }
        }

        public async Task<string> SendToken(string recipientBase58, ulong amount, string mintAddress, int decimals)
        {
            if (MainAddress == null) throw new Exception("Wallet not connected");

            var feePayer = new PublicKey(MainAddress);
            var mint = new PublicKey(mintAddress);
            var recipient = new PublicKey(recipientBase58);

            var senderAta = SolanaTokenService.FindAssociatedTokenAddress(feePayer, mint);
            var recipientAta = SolanaTokenService.FindAssociatedTokenAddress(recipient, mint);

            var txBuilder = new TransactionBuilder()
                .SetFeePayer(feePayer);

            var recipientAtaInfo = await _rpcClient.GetAccountInfoAsync(recipientAta.Key);
            if (!recipientAtaInfo.WasSuccessful || recipientAtaInfo.Result.Value == null)
            {
                txBuilder.AddInstruction(SolanaTokenService.CreateAssociatedTokenAccountInstruction(feePayer, recipient, mint));
            }

            txBuilder.AddInstruction(SolanaTokenService.CreateTransferCheckedInstruction(
                senderAta, mint, recipientAta, feePayer, amount, (byte)decimals
            ));

            return await SignAndSendTransaction(txBuilder);
        }

        public async Task<string> SendSol(string recipientBase58, ulong lamports)
        {
            if (MainAddress == null) throw new Exception("Wallet not connected");

            var feePayer = new PublicKey(MainAddress);
            var txBuilder = new TransactionBuilder()
                .SetFeePayer(feePayer)
                .AddInstruction(SystemProgram.Transfer(feePayer, new PublicKey(recipientBase58), lamports));

            return await SignAndSendTransaction(txBuilder);
        }
    }
}
