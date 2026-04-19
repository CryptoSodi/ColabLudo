using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SignalR.Server.Interfaces;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Rpc.Types;
using Solnet.Wallet;
using System.Text.Json;
namespace SignalR.Server.Payments
{
    public class LudcPaymentProvider(IDbContextFactory<LudoDbContext> _contextFactory, IDataProtectionProvider dataProtectionProvider, SolPaymentProvider solPaymentProvider, int _masterUserId, bool debug, string purpose, string LUDC_MINT_ADDRESS, string rpcUrl) : IPaymentProvider
    {
        public CurrencyType Currency => CurrencyType.LUDC;
        public string MintAddress => LUDC_MINT.Key;
        private readonly IRpcClient _rpc = string.IsNullOrEmpty(rpcUrl) 
            ? ClientFactory.GetClient(debug ? Cluster.DevNet : Cluster.MainNet) 
            : ClientFactory.GetClient(rpcUrl); 
        private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(purpose);                         // Data protector for encrypt/decrypt        

        private const int LUDC_DECIMALS = 9;
        private readonly PublicKey LUDC_MINT = new PublicKey(LUDC_MINT_ADDRESS);

        private readonly PublicKey TOKEN_2022_PROGRAM = new PublicKey("TokenzQdBNbLqP5VEhdkAS6EPFLC1PHnBqCXEpPxuEb");

        /* =========================================================
        * WALLET INITIALIZATION (SINGLE SOURCE OF TRUTH)
        * ========================================================= */
        public async Task<PlayerWallet> EnsurePlayerWalletExists(int playerId, string addressType = "LUDC")
        {
            return await solPaymentProvider.EnsurePlayerWalletExists(playerId, addressType);
        }
        // ================= WITHDRAW =================
        public async Task<string> WithdrawAsync(Player player, string destination, decimal amount, Guid operationId)
        {
            using var ctx = _contextFactory.CreateDbContext();
            using var tx = await ctx.Database.BeginTransactionAsync();

            var wallet = await EnsurePlayerWalletExists(player.PlayerId);
            if (wallet.AvailableBalance < amount)
                return "INSUFFICIENT_BALANCE";
            var masterKey = await ctx.PlayerWalletKey.FirstAsync(x => x.PlayerId == _masterUserId);
            var sig = await SendLudcAsync(masterKey, destination, amount);
            if(sig != "ERROR")
            {
                wallet.AvailableBalance -= amount;
                ctx.Update(wallet);
                await ctx.SaveChangesAsync();
                await tx.CommitAsync();
            }
            return sig;
        }
        // ================= SWEEP =================
        public async Task<string> SweepAsync(int playerId, decimal amount)
        {
            using var ctx = _contextFactory.CreateDbContext();

            var playerKey = await ctx.PlayerWalletKey.FirstAsync(x => x.PlayerId == playerId);
            var masterKey = await ctx.PlayerWalletKey.FirstAsync(x => x.PlayerId == _masterUserId);
            return await SendLudcAsync(playerKey, masterKey.PublicKey, amount);
        }
        // ================= BALANCE =================
        public async Task<decimal> GetOnChainBalanceAsync(string walletAddress)
        {
            var owner = new PublicKey(walletAddress);
            var ata = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(owner, LUDC_MINT);
            var balance = await _rpc.GetTokenAccountBalanceAsync(ata);
            if (!balance.WasSuccessful || balance.Result?.Value == null)
                return 0m;
            return decimal.Parse(balance.Result.Value.UiAmountString);
        }
        public async Task<object> PrepareDepositFromExternalWalletAsync(string senderWalletAddress, string destinationOwnerAddress, decimal amount)
        {
            var senderPub = new PublicKey(senderWalletAddress);
            var receiverPub = new PublicKey(destinationOwnerAddress);

            PublicKey.TryFindProgramAddress(
                new[] { senderPub.KeyBytes, TOKEN_2022_PROGRAM.KeyBytes, LUDC_MINT.KeyBytes },
                AssociatedTokenAccountProgram.ProgramIdKey,
                out var senderAta,
                out _);

            PublicKey.TryFindProgramAddress(
                new[] { receiverPub.KeyBytes, TOKEN_2022_PROGRAM.KeyBytes, LUDC_MINT.KeyBytes },
                AssociatedTokenAccountProgram.ProgramIdKey,
                out var receiverAta,
                out _);

            var senderAtaInfo = await _rpc.GetAccountInfoAsync(senderAta);
            var receiverAtaInfo = await _rpc.GetAccountInfoAsync(receiverAta);
            var blockhash = (await _rpc.GetLatestBlockHashAsync()).Result.Value.Blockhash;
            ulong tokenAmount = Convert.ToUInt64(decimal.Round(amount * 1_000_000_000m, 0, MidpointRounding.AwayFromZero));

            return new
            {
                SenderOwner = senderWalletAddress,
                DestinationOwner = destinationOwnerAddress,
                SenderAta = senderAta.Key,
                DestinationAta = receiverAta.Key,
                SenderAtaExists = senderAtaInfo.WasSuccessful && senderAtaInfo.Result?.Value != null,
                DestinationAtaExists = receiverAtaInfo.WasSuccessful && receiverAtaInfo.Result?.Value != null,
                Blockhash = blockhash,
                Mint = LUDC_MINT.Key,
                TokenProgram = TOKEN_2022_PROGRAM.Key,
                AssociatedTokenProgram = AssociatedTokenAccountProgram.ProgramIdKey.Key,
                SystemProgram = SystemProgram.ProgramIdKey.Key,
                Decimals = LUDC_DECIMALS,
                AmountRaw = tokenAmount.ToString()
            };
        }
        public async Task<bool> ConfirmSignatureAsync(string signature)
        {
            if (string.IsNullOrWhiteSpace(signature)) return false;

            try 
            {
                // Poll for confirmation (Max 30 seconds)
                for (int i = 0; i < 15; i++)
                {
                    var tx = await _rpc.GetTransactionAsync(signature, Commitment.Confirmed);
                    if (tx.WasSuccessful && tx.Result != null)
                    {
                        // Check if the transaction actually succeeded or if it reverted
                        bool hasError = tx.Result.Meta.Error != null;
                        if (!hasError)
                        {
                            return true;
                        }
                        else 
                        {
                            Console.WriteLine($"[LudcProvider] Signature {signature} found but FAILED on-chain.");
                            return false;
                        }
                    }
                    await Task.Delay(2000); // Wait 2 seconds between checks
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LudcProvider] Error during confirmation: {ex.Message}");
                return false;
            }
        }

        public async Task<BroadcastResult> BroadcastTransactionAsync(string txBase64)
        {
            try
            {
                string cleanTx = txBase64.Trim('\"', ' ', '\n', '\r');
                byte[] txBytes = Convert.FromBase64String(cleanTx);
                
                Console.WriteLine($"[LudcProvider] Broadcasting {txBytes.Length} bytes...");
                
                // Try Solnet first with skipPreflight = true for speed and flexibility
                var res = await _rpc.SendTransactionAsync(txBytes, skipPreflight: true);
                
                if (res.WasSuccessful) 
                    return new BroadcastResult { WasSuccessful = true, Result = res.Result };

                // Handle common failure: Solnet fails to parse HTML/Non-JSON response from rate-limited nodes
                if (res.Reason != null && (res.Reason.Contains("json", StringComparison.OrdinalIgnoreCase) || res.Reason.Contains("simulation", StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine($"[LudcProvider] Solnet error ({res.Reason}). Attempting manual RPC call to capture raw error...");
                    
                    var rpcRequest = new
                    {
                        jsonrpc = "2.0",
                        id = 1,
                        method = "sendTransaction",
                        @params = new object[] { cleanTx, new { skipPreflight = true, encoding = "base64" } }
                    };

                    string targetUrl = rpcUrl ?? "https://api.mainnet-beta.solana.com";
                    using var httpRes = await _http.PostAsJsonAsync(targetUrl, rpcRequest);
                    var content = await httpRes.Content.ReadAsStringAsync();
                    
                    if (httpRes.IsSuccessStatusCode || !string.IsNullOrEmpty(content))
                    {
                        try 
                        {
                            using var doc = JsonDocument.Parse(content);
                            if (doc.RootElement.TryGetProperty("result", out var sig))
                            {
                                return new BroadcastResult { WasSuccessful = true, Result = sig.GetString() };
                            }
                            
                            if (doc.RootElement.TryGetProperty("error", out var err))
                            {
                                string msg = err.TryGetProperty("message", out var m) ? m.GetString() : "Unknown RPC Error";
                                
                                // --- ERROR TRANSLATION LOGIC ---
                                if (msg.Contains("insufficient funds for rent", StringComparison.OrdinalIgnoreCase))
                                {
                                    msg = "Insufficient SOL in your wallet to pay for account rent. Please add at least 0.003 SOL and try again.";
                                }
                                else if (msg.Contains("insufficient lamports", StringComparison.OrdinalIgnoreCase))
                                {
                                    msg = "Insufficient SOL balance to pay for transaction gas fees.";
                                }
                                else if (msg.Contains("Blockhash not found", StringComparison.OrdinalIgnoreCase))
                                {
                                    msg = "Transaction expired. Please try again immediately.";
                                }
                                
                                Console.WriteLine($"[LudcProvider] Translated RPC error: {msg}");
                                return new BroadcastResult { WasSuccessful = false, Reason = msg };
                            }
                        }
                        catch { /* Fallback to raw content if JSON parse fails */ }
                    }
                    return new BroadcastResult { WasSuccessful = false, Reason = $"RPC Error {httpRes.StatusCode}: {content}" };
                }

                return new BroadcastResult { WasSuccessful = false, Reason = res.Reason };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LudcProvider] Broadcast Exception: {ex.Message}");
                return new BroadcastResult { WasSuccessful = false, Reason = ex.Message };
            }
        }
        // ================= INTERNAL SEND =================
        private async Task<string> SendLudcAsync(PlayerWalletKey senderKey, string destination, decimal amount)
        {
            var rawPriv = _protector.Unprotect(senderKey.EncryptedPrivateKey);
            var senderAccount = new Account(rawPriv, senderKey.PublicKey);
            var receiverPub = new PublicKey(destination);
            // ================= DERIVE ATA =================
            PublicKey.TryFindProgramAddress(new[]{senderAccount.PublicKey.KeyBytes,TOKEN_2022_PROGRAM.KeyBytes,LUDC_MINT.KeyBytes},
                AssociatedTokenAccountProgram.ProgramIdKey,out var senderAta,out _);
            PublicKey.TryFindProgramAddress(new[]{receiverPub.KeyBytes,TOKEN_2022_PROGRAM.KeyBytes,LUDC_MINT.KeyBytes},
                AssociatedTokenAccountProgram.ProgramIdKey,out var receiverAta,out _);
            // ================= CHECK RECEIVER ATA =================
            var receiverAtaInfo = await _rpc.GetAccountInfoAsync(receiverAta);
            bool receiverAtaExists = receiverAtaInfo.WasSuccessful && receiverAtaInfo.Result?.Value != null;
            // ================= TOKEN AMOUNT =================
            ulong tokenAmount = Convert.ToUInt64(decimal.Round(amount * 1_000_000_000m,0,MidpointRounding.AwayFromZero));
            var blockhash = (await _rpc.GetLatestBlockHashAsync()).Result.Value.Blockhash;
            // ================= BUILD TRANSACTION =================
            var builder = new TransactionBuilder().SetRecentBlockHash(blockhash).SetFeePayer(senderAccount.PublicKey);
            // ================= CREATE ATA IF MISSING =================
            if (!receiverAtaExists)
            {
                var createAtaInstruction = new TransactionInstruction
                {
                    ProgramId = AssociatedTokenAccountProgram.ProgramIdKey, Keys = new List<AccountMeta>
                    {
                        AccountMeta.Writable(senderAccount.PublicKey, true),     // payer        
                        AccountMeta.Writable(receiverAta, false),                // ATA        
                        AccountMeta.ReadOnly(receiverPub, false),                // owner        
                        AccountMeta.ReadOnly(LUDC_MINT, false),                  // mint        
                        AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false), // system program        
                        AccountMeta.ReadOnly(TOKEN_2022_PROGRAM, false)          // token program    
                    },
                    Data = Array.Empty<byte>() // ATA create has empty data
                };
                builder.AddInstruction(createAtaInstruction);
            }
            // ================= TRANSFER INSTRUCTION =================
            var data = new List<byte> {12};
            var amountBytes = new byte[8];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(amountBytes, tokenAmount);
            data.AddRange(amountBytes);
            data.Add((byte)LUDC_DECIMALS);
            var transferInstruction = new TransactionInstruction
            {
                ProgramId = TOKEN_2022_PROGRAM,
                Keys = new List<AccountMeta>
                {            
                    AccountMeta.Writable(senderAta, false),            
                    AccountMeta.ReadOnly(LUDC_MINT, false),            
                    AccountMeta.Writable(receiverAta, false),            
                    AccountMeta.ReadOnly(senderAccount.PublicKey, true)        
                },
                Data = data.ToArray()
            };
            builder.AddInstruction(transferInstruction);
            // ================= BUILD TX =================
            var tx = builder.Build(senderAccount);
            // ================= SEND TX =================
            var res = await _rpc.SendTransactionAsync(tx, false, Commitment.Confirmed);
            if (!res.WasSuccessful)
            {
                Console.WriteLine($"SEND FAILED : {res.Reason}");
                return "ERROR";
            }
            else
                Console.WriteLine($"SEND SUCCESS : {res.Result}");
                return "SUCCESS";
        }
        public async Task<List<TokenDeposit>> GetRecentDeposits(string? lastProcessedSignature = null)
        {
            var deposits = new List<TokenDeposit>();

            // Fetch newest signatures
            var sigs = await _rpc.GetSignaturesForAddressAsync(LUDC_MINT,until: lastProcessedSignature,limit: 100);

            if (!sigs.WasSuccessful || sigs.Result == null || sigs.Result.Count == 0)
                return deposits;
            
            sigs.Result.Reverse();

            foreach (var sig in sigs.Result)
            {
                var tx = await _rpc.GetTransactionAsync(sig.Signature,Commitment.Confirmed);

                if (tx == null)
                    continue;

                var meta = tx.Result.Meta;

                if (meta?.PostTokenBalances == null)
                    continue;

                foreach (var balance in meta.PostTokenBalances)
                {
                    // Only track LUDC token
                    if (balance.Mint != LUDC_MINT.Key)
                        continue;

                    var accountIndex = balance.AccountIndex;

                    var tokenAccountAddress =
                        tx.Result.Transaction.Message.AccountKeys[accountIndex];

                    var pre = meta.PreTokenBalances?
                        .FirstOrDefault(x => x.AccountIndex == accountIndex);

                    // Ignore mint events (no previous balance)
                    if (pre == null)
                        continue;

                    // Use raw amount and divide by 1m as per user fix
                    if (!decimal.TryParse(balance.UiTokenAmount.Amount, out decimal postRaw)) continue;
                    decimal preRaw = 0;
                    if (pre != null && decimal.TryParse(pre.UiTokenAmount.Amount, out var p)) preRaw = p;

                    decimal delta = (postRaw - preRaw) / 1m;

                    // Only detect deposits
                    if (delta <= 0)
                        continue;

                    Console.WriteLine($"[LudcProvider] Deposit Detected! Sig: {sig.Signature} | Crediting: {delta} LUDC");

                    // Fetch ATA account info
                    var accountInfo = await _rpc.GetAccountInfoAsync(tokenAccountAddress);


                    if (!accountInfo.WasSuccessful || accountInfo.Result?.Value == null)
                        continue;

                    var data = Convert.FromBase64String(accountInfo.Result.Value.Data[0]);

                    // SPL token account layout
                    var ownerBytes = data.Skip(32).Take(32).ToArray();

                    var owner = new PublicKey(ownerBytes).Key;

                    deposits.Add(new TokenDeposit
                    {
                        Signature = sig.Signature,
                        WalletAddress = owner,
                        Amount = delta
                    });
                }
            }        
            return deposits;
        }
        private static readonly HttpClient _http = new HttpClient();
        private async Task<JsonDocument?> GetTransactionRaw(string signature)
        {
            var request = new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "getTransaction",
                @params = new object[]
                {
            signature,
            new
            {
                encoding = "jsonParsed",
                commitment = "confirmed",
                maxSupportedTransactionVersion = 0
            }
                }
            };

            var response = await _http.PostAsJsonAsync(
                string.IsNullOrEmpty(rpcUrl) ? "https://api.mainnet-beta.solana.com" : rpcUrl,
                request);

            var json = await response.Content.ReadAsStringAsync();

            var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("result", out var result) || result.ValueKind == JsonValueKind.Null)
                return null;

            return doc;
        }
        public async Task<string> InitializeLatestSignature()
        {
            var sigs = await _rpc.GetSignaturesForAddressAsync(LUDC_MINT, limit: 1);

            if (sigs.WasSuccessful && sigs.Result?.Count > 0)
            {
               string _lastProcessedSignature = sigs.Result[0].Signature;

                Console.WriteLine($"Deposit scanner starting from: {_lastProcessedSignature}");
                return _lastProcessedSignature;
            }
            return null;
        }
    }
    public class TokenDeposit
    {
        public string Signature { get; set; } = "";
        public string WalletAddress { get; set; } = "";
        public decimal Amount { get; set; }
    }

    public class BroadcastResult
    {
        public bool WasSuccessful { get; set; }
        public string Result { get; set; } = "";
        public string Reason { get; set; } = "";
    }
}
