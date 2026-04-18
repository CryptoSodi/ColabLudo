using CommunityToolkit.Maui.Alerts;
using Org.BouncyCastle.Asn1.Ocsp;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Wallet;
using static Org.BouncyCastle.Bcpg.Attr.ImageAttrib;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        public string? AuthToken = "";

        public List<AccountDetails> Accounts = new();
        public byte[]? MainAddress => Accounts.FirstOrDefault()?.PublicKey;
        public string? MainAddressBase58 
        {
            get
            {
                var acc = Accounts.FirstOrDefault();
                if (acc == null) return null;
                if (!string.IsNullOrEmpty(acc.DisplayAddress)) return acc.DisplayAddress;
                try {
                    return new Solnet.Wallet.PublicKey(acc.PublicKey).Key;
                } catch {
                    return null;
                }
            }
        }
        public double SolBalance { get; private set; }
        public List<TokenBalance> TokenBalances { get; private set; } = new();

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
                
                // Fetch SOL balance
                var balanceResult = await _rpcClient.GetBalanceAsync(MainAddressBase58);
                if (balanceResult.WasSuccessful)
                {
                    SolBalance = (double)balanceResult.Result.Value / 1_000_000_000.0;
                }

                var newList = new List<TokenBalance>();
                var owner = new PublicKey(MainAddressBase58);
                bool isMainnet = _clusterName == "mainnet-beta";

                // Explicit Token Fetching Task (LUDC and USDC)
                async Task FetchToken(string mintAddr, string symbol)
                {
                    try {
                        var ata = SolanaTokenService.FindAssociatedTokenAddress(owner, new PublicKey(mintAddr));
                        var bal = await _rpcClient.GetTokenAccountBalanceAsync(ata.ToString());
                        if (bal.WasSuccessful && bal.Result?.Value != null)
                        {
                            newList.Add(new TokenBalance {
                                Mint = mintAddr,
                                Amount = bal.Result.Value.AmountDecimal,
                                Decimals = bal.Result.Value.Decimals,
                                Symbol = symbol
                            });
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

        /// <summary>
        /// Centralized helper to handle the MWA 65-byte header, signing, and broadcasting.
        /// </summary>
        private async Task<string> SignAndSendTransaction(TransactionBuilder txBuilder)
        {
            var blockhashResult = await _rpcClient.GetLatestBlockHashAsync();
            if (!blockhashResult.WasSuccessful) throw new Exception("Failed to get blockhash");

            txBuilder.SetRecentBlockHash(blockhashResult.Result.Value.Blockhash);
            var msgBytes = txBuilder.CompileMessage();

            // MWA requires a 65-byte buffer: [1 byte for signature count (usually 1)] + [64 zero bytes for sig space] + [message bytes]
            var txBytes = new byte[1 + 64 + msgBytes.Length];
            txBytes[0] = 1; // Number of signatures
            Array.Copy(msgBytes, 0, txBytes, 65, msgBytes.Length);

            var auth = await AuthorizeOrReauthorize();
            if (auth == null) 
                throw new Exception("Wallet not connected");

            var signResult = await _connection.Client!.SignTransactions(new List<byte[]> { txBytes });
            if (signResult == null || signResult.SignedPayloads.Count == 0) throw new Exception("Signature failed or declined");

            var txSignature = await _rpcClient.SendTransactionAsync(signResult.SignedPayloadsBytes[0]);
            if (!txSignature.WasSuccessful) throw new Exception($"Broadcast failed: {txSignature.Reason}");

            return txSignature.Result;
        }

        public async Task<string> SendToken(string recipientBase58, ulong amount, string mintAddress, int decimals)
        {
            if (MainAddress == null)
                throw new Exception("Wallet not connected");


            var blockhashResult = await _rpcClient.GetLatestBlockHashAsync();
            if (!blockhashResult.WasSuccessful || blockhashResult.Result == null)
                throw new Exception($"Failed to get latest blockhash: {blockhashResult.Reason}");

            var feePayer = new PublicKey(MainAddress);
            var mint = new PublicKey(mintAddress);
            var recipient = new PublicKey(recipientBase58);

            var senderAta = SolanaTokenService.FindAssociatedTokenAddress(feePayer, mint);
            var recipientAta = SolanaTokenService.FindAssociatedTokenAddress(recipient, mint);

            Console.WriteLine($"[WMA] Sending {amount} tokens. Sender ATA: {senderAta}, Recipient ATA: {recipientAta}");

            var txBuilder = new TransactionBuilder()
                .SetRecentBlockHash(blockhashResult.Result.Value.Blockhash)
                .SetFeePayer(feePayer);
            var recipientAtaInfo = await _rpcClient.GetAccountInfoAsync(recipientAta.Key);
            if (!recipientAtaInfo.WasSuccessful || recipientAtaInfo.Result.Value == null)
            {
                Console.WriteLine("[WMA] Recipient ATA not found. Adding creation instruction...");
                txBuilder.AddInstruction(SolanaTokenService.CreateAssociatedTokenAccountInstruction(feePayer, recipient, mint));
            }

            txBuilder.AddInstruction(SolanaTokenService.CreateTransferCheckedInstruction(
                senderAta, mint, recipientAta, feePayer, amount, (byte)decimals
            ));
            // Build the transaction message
            var msgBytes = txBuilder.CompileMessage();

            // Manually construct the Transaction wire format for WMA:
            // signature_count (1) + signature (64 zero bytes) + message
            var txBytes = new byte[1 + 64 + msgBytes.Length];
            txBytes[0] = 1; // 1 signature slot
            Array.Copy(msgBytes, 0, txBytes, 65, msgBytes.Length);

            var auth = await AuthorizeOrReauthorize();
            if (auth == null)
                throw new Exception("Wallet not connected");

            Console.WriteLine("[WMA] Requesting signature from wallet...");
            var signResult = await _connection.Client!.SignTransactions(new List<byte[]> { txBytes });
            if (signResult == null || signResult.SignedPayloads.Count == 0)
                throw new Exception("Signature failed or declined");

            Console.WriteLine($"[WMA] Received sign result. Payloads count: {signResult?.SignedPayloads?.Count ?? 0}");
            Console.WriteLine("[WMA] Broadcasting signed transaction...");
            var txSignature = await _rpcClient.SendTransactionAsync(signResult.SignedPayloads[0]);

            if (!txSignature.WasSuccessful) 
                throw new Exception($"Broadcast failed: {txSignature.Reason}");
            return txSignature.Result;
        }

        public async Task<string> SendSol(string recipientBase58, ulong lamports)
        {
            await Connect();
            if (MainAddress == null) throw new Exception("Wallet not connected");

            var feePayer = new PublicKey(MainAddress);
            var txBuilder = new TransactionBuilder()
                .SetFeePayer(feePayer)
                .AddInstruction(SystemProgram.Transfer(feePayer, new PublicKey(recipientBase58), lamports));

            return await SignAndSendTransaction(txBuilder);
        }
    }
}
