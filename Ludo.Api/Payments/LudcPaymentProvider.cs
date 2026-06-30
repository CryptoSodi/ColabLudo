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
using System.Net.Http.Json;

namespace SignalR.Server.Payments
{
    public class LudcPaymentProvider(IDbContextFactory<LudoDbContext> _contextFactory, IDataProtectionProvider dataProtectionProvider, SolPaymentProvider solPaymentProvider, int _masterUserId, bool debug, string purpose, string LUDC_MINT_ADDRESS, string rpcUrl, string jupiterBaseUrl, string jupiterApiKey) : IPaymentProvider
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
        * ATA CREATION FEE (paid by the withdrawing player, in LUDC)
        * ---------------------------------------------------------
        * When a withdrawal forces the treasury to create the recipient's
        * LUDC token account, the treasury pays ~0.00207408 SOL of rent.
        * We charge that cost back to the player, priced live in LUDC, so
        * the treasury is never the one funding a receiver's account.
        * ========================================================= */
        private const long ATA_ACCOUNT_SIZE = 170;                  // Token-2022 ATA: 165 base + account-type byte + ImmutableOwner TLV
        private const ulong ATA_RENT_FALLBACK_LAMPORTS = 2_074_080; // rent-exempt minimum for 170 bytes (used if RPC is unreachable)
        private const decimal FEE_MARGIN = 1.2m;                    // buffer for spread/slippage when LUDC fees are later converted to SOL
        private const int FEE_CACHE_SECONDS = 60;                   // how long a live LUDC price is reused before re-quoting
        private const string WRAPPED_SOL_MINT = "So11111111111111111111111111111111111111112";
        private decimal _cachedFeeLudc = 0m;
        private DateTime _feeQuoteTime = DateTime.MinValue;

        // Non-signature results WithdrawAsync can return. Callers treat anything
        // NOT in this set as a successful on-chain signature / internal ref.
        public static readonly HashSet<string> WithdrawErrorCodes = new()
        {
            "ERROR", "WALLET_NOT_FOUND", "SELF_TRANSFER_NOT_ALLOWED",
            "INSUFFICIENT_BALANCE", "INSUFFICIENT_BALANCE_FOR_FEE",
            "FEE_UNAVAILABLE", "AMOUNT_TOO_SMALL_FOR_FEE"
        };

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

            var wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == player.PlayerId && w.AddressType == "LUDC");
            if (wallet == null)
            {
                await EnsurePlayerWalletExists(player.PlayerId);
                wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == player.PlayerId && w.AddressType == "LUDC");
            }

            if (wallet == null)
                return "WALLET_NOT_FOUND";

            if (wallet.AvailableBalance < amount)
                return "INSUFFICIENT_BALANCE";

            var recipientWallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.WalletAddress == destination && w.AddressType == "LUDC");
            if (recipientWallet != null)
            {
                if (recipientWallet.PlayerId == player.PlayerId)
                    return "SELF_TRANSFER_NOT_ALLOWED";

                wallet.AvailableBalance -= amount;
                recipientWallet.AvailableBalance += amount;

                var senderTransferRef = $"internal:withdraw:{Guid.NewGuid()}";
                var recipientTransferRef = $"internal:deposit:{Guid.NewGuid()}";
                ctx.WalletTransaction.Add(new WalletTransaction
                {
                    PlayerId = wallet.PlayerId,
                    OperationId = Guid.NewGuid(),
                    Amount = -amount,
                    BalanceAfter = wallet.AvailableBalance,
                    Type = TransactionType.Withdrawal,
                    Status = WalletTransactionStatus.Completed,
                    Description = $"Internal transfer to player {recipientWallet.PlayerId}",
                    IsOnChain = false,
                    txId = senderTransferRef,
                    AddressType = "LUDC"
                });

                ctx.WalletTransaction.Add(new WalletTransaction
                {
                    PlayerId = recipientWallet.PlayerId,
                    OperationId = Guid.NewGuid(),
                    Amount = amount,
                    BalanceAfter = recipientWallet.AvailableBalance,
                    Type = TransactionType.Deposit,
                    Status = WalletTransactionStatus.Completed,
                    Description = $"Internal transfer from player {wallet.PlayerId}",
                    IsOnChain = false,
                    txId = recipientTransferRef,
                    AddressType = "LUDC"
                });

                ctx.Update(wallet);
                ctx.Update(recipientWallet);
                await ctx.SaveChangesAsync();
                await tx.CommitAsync();

                Console.WriteLine($"[LudcProvider] Internal transfer completed: Player {wallet.PlayerId} -> Player {recipientWallet.PlayerId} Amount: {amount} LUDC");
                return senderTransferRef;
            }

            var masterKey = await ctx.PlayerWalletKey.FirstAsync(x => x.PlayerId == _masterUserId);

            // Does the treasury have to create (rent-fund) the destination's LUDC
            // account? If so, charge the player the SOL rent cost, priced live in
            // LUDC. Existing accounts cost no rent, so they pay no fee.
            var receiverPub = new PublicKey(destination);
            PublicKey.TryFindProgramAddress(
                new[] { receiverPub.KeyBytes, TOKEN_2022_PROGRAM.KeyBytes, LUDC_MINT.KeyBytes },
                AssociatedTokenAccountProgram.ProgramIdKey, out var receiverAta, out _);
            var receiverAtaInfo = await _rpc.GetAccountInfoAsync(receiverAta);
            bool receiverAtaExists = receiverAtaInfo.WasSuccessful && receiverAtaInfo.Result?.Value != null;

            decimal feeLudc = 0m;
            if (!receiverAtaExists)
            {
                var quotedFee = await GetAtaCreationFeeLudcAsync(masterKey.PublicKey);
                if (quotedFee == null)
                    return "FEE_UNAVAILABLE";          // could not price the rent live -> fail closed, never fund a receiver for free
                feeLudc = quotedFee.Value;

                if (amount <= feeLudc)
                    return "AMOUNT_TOO_SMALL_FOR_FEE";  // withdrawal worth less than the account-creation fee
            }

            if (wallet.AvailableBalance < amount + feeLudc)
                return "INSUFFICIENT_BALANCE_FOR_FEE";

            var sig = await SendLudcAsync(masterKey, destination, amount);
            if (sig != "ERROR")
            {
                // Ledger: the withdrawal itself
                wallet.AvailableBalance -= amount;
                ctx.WalletTransaction.Add(new WalletTransaction
                {
                    PlayerId = wallet.PlayerId,
                    OperationId = operationId,
                    Amount = -amount,
                    BalanceAfter = wallet.AvailableBalance,
                    Type = TransactionType.Withdrawal,
                    Status = WalletTransactionStatus.Completed,
                    Description = $"Withdraw to {destination}",
                    IsOnChain = true,
                    txId = sig,
                    AddressType = "LUDC"
                });

                // Ledger: ATA account creation fee (player pays -> treasury keeps)
                if (feeLudc > 0m)
                {
                    wallet.AvailableBalance -= feeLudc;
                    ctx.WalletTransaction.Add(new WalletTransaction
                    {
                        PlayerId = wallet.PlayerId,
                        OperationId = Guid.NewGuid(),
                        Amount = -feeLudc,
                        BalanceAfter = wallet.AvailableBalance,
                        Type = TransactionType.Fee,
                        Status = WalletTransactionStatus.Completed,
                        Description = "ATA account creation fee",
                        IsOnChain = false,
                        txId = sig,
                        AddressType = "LUDC"
                    });

                    // The fee LUDC never leaves the on-chain pool, so credit the
                    // treasury's internal claim to keep DB balances reconciled and
                    // give a running total of fees available to convert back to SOL.
                    var masterWallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == _masterUserId && w.AddressType == "LUDC");
                    if (masterWallet != null)
                    {
                        masterWallet.AvailableBalance += feeLudc;
                        ctx.WalletTransaction.Add(new WalletTransaction
                        {
                            PlayerId = _masterUserId,
                            OperationId = Guid.NewGuid(),
                            Amount = feeLudc,
                            BalanceAfter = masterWallet.AvailableBalance,
                            Type = TransactionType.Fee,
                            Status = WalletTransactionStatus.Completed,
                            Description = $"ATA account creation fee from player {wallet.PlayerId}",
                            IsOnChain = false,
                            txId = sig,
                            AddressType = "LUDC"
                        });
                        ctx.Update(masterWallet);
                    }
                }

                ctx.Update(wallet);
                await ctx.SaveChangesAsync();
                await tx.CommitAsync();

                Console.WriteLine($"[LudcProvider] Withdraw complete: Player {wallet.PlayerId} -> {destination} Amount: {amount} LUDC, Fee: {feeLudc} LUDC (ATA {(receiverAtaExists ? "existed" : "created")})");
            }
            return sig;
        }
        // ================= ATA CREATION FEE (LIVE PRICE) =================
        // Returns the LUDC fee equal to the SOL rent of creating a recipient's
        // LUDC token account, priced via a live Jupiter quote (no swap executed).
        // Returns null if the price cannot be obtained and no recent value is cached.
        private async Task<decimal?> GetAtaCreationFeeLudcAsync(string takerPubkey)
        {
            if ((DateTime.UtcNow - _feeQuoteTime).TotalSeconds < FEE_CACHE_SECONDS && _cachedFeeLudc > 0m)
                return _cachedFeeLudc;

            // 1) Actual on-chain rent for a LUDC (Token-2022) ATA
            ulong rentLamports = ATA_RENT_FALLBACK_LAMPORTS;
            try
            {
                var rentRes = await _rpc.GetMinimumBalanceForRentExemptionAsync(ATA_ACCOUNT_SIZE);
                if (rentRes.WasSuccessful && rentRes.Result > 0)
                    rentLamports = rentRes.Result;
            }
            catch { /* keep fallback */ }

            // 2) Live value of that rent in LUDC (Jupiter quote only, nothing executed)
            try
            {
                string url = $"{jupiterBaseUrl.TrimEnd('/')}/swap/v2/order" +
                             $"?inputMint={WRAPPED_SOL_MINT}&outputMint={LUDC_MINT.Key}" +
                             $"&amount={rentLamports}&slippageBps=100&taker={takerPubkey}";
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                if (!string.IsNullOrWhiteSpace(jupiterApiKey))
                    req.Headers.Add("x-api-key", jupiterApiKey);
                using var res = await _http.SendAsync(req);
                var body = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("outAmount", out var outAmt))
                {
                    string raw = outAmt.ValueKind == JsonValueKind.String ? outAmt.GetString() ?? "" : outAmt.GetRawText();
                    if (ulong.TryParse(raw, out var ludcRaw) && ludcRaw > 0)
                    {
                        decimal ludc = ludcRaw / 1_000_000_000m; // LUDC has 9 decimals
                        _cachedFeeLudc = decimal.Round(ludc * FEE_MARGIN, 8, MidpointRounding.AwayFromZero);
                        _feeQuoteTime = DateTime.UtcNow;
                        Console.WriteLine($"[LudcProvider] ATA fee priced: rent={rentLamports} lamports -> {_cachedFeeLudc} LUDC");
                        return _cachedFeeLudc;
                    }
                }
                Console.WriteLine($"[LudcProvider] Fee quote missing outAmount. Body: {body}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LudcProvider] Fee quote failed: {ex.Message}");
            }

            // Live data unavailable: reuse a recent value if we have one, else signal failure.
            return _cachedFeeLudc > 0m ? _cachedFeeLudc : (decimal?)null;
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
            {
                Console.WriteLine($"SEND SUCCESS : {res.Result}");
                return res.Result;
            }
        }
        public async Task<ScannerResult> GetRecentDeposits(string? lastProcessedSignature = null)
        {
            var result = new ScannerResult();
            var signatures = new List<string>();

            // 1. DEEP CATCH-UP: Fetch ALL signatures since the last processed one
            string? beforeMarker = null;
            bool foundOldMarker = false;
            int maxSafety = 0;

            try {
                while (!foundOldMarker && maxSafety < 10) 
                {
                    var rpcRequest = new {
                        jsonrpc = "2.0", id = 1, method = "getSignaturesForAddress",
                        @params = new object[] { LUDC_MINT.Key, new { limit = 10, until = lastProcessedSignature, before = beforeMarker } }
                    };
                    using var httpRes = await _http.PostAsJsonAsync("https://api.mainnet-beta.solana.com", rpcRequest);
                    var content = await httpRes.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    
                    if (doc.RootElement.TryGetProperty("result", out var resArr) && resArr.ValueKind == JsonValueKind.Array) 
                    {
                        var batch = resArr.EnumerateArray().ToList();
                        if (batch.Count == 0) break;
                        foreach (var s in batch) signatures.Add(s.GetProperty("signature").GetString());
                        if (batch.Count < 10) foundOldMarker = true;
                        else beforeMarker = batch.Last().GetProperty("signature").GetString();
                    }
                    else break;
                    maxSafety++;
                }
            } catch (Exception ex) {
                Console.WriteLine($"[LudcProvider] Signature Catch-up Failed: {ex.Message}");
            }

            if (signatures.Count == 0) return result;
            result.LatestSeenSignature = signatures.First();

            Console.WriteLine($"[LudcProvider] Scanning {signatures.Count} new signatures...");
            signatures.Reverse(); 

            foreach (var sig in signatures)
            {
                try {
                    Console.WriteLine($"[LudcProvider] Fetching details for: {sig}");
                    await Task.Delay(500);

                    var txDoc = await GetTransactionRaw(sig);
                    if (txDoc == null) continue;

                    var root = txDoc.RootElement;
                    if (!root.TryGetProperty("result", out var txResult) || txResult.ValueKind == JsonValueKind.Null) continue;
                    if (!txResult.TryGetProperty("meta", out var meta)) continue;
                    if (!meta.TryGetProperty("postTokenBalances", out var postBalances)) continue;

                    foreach (var balance in postBalances.EnumerateArray())
                    {
                        if (balance.GetProperty("mint").GetString() != LUDC_MINT.Key) continue;

                        var uiAmount = balance.GetProperty("uiTokenAmount");
                        if (!decimal.TryParse(uiAmount.GetProperty("uiAmountString").GetString(), out decimal postAmt)) continue;
                        
                        int accountIndex = balance.GetProperty("accountIndex").GetInt32();
                        decimal preAmt = 0m;
                        if (meta.TryGetProperty("preTokenBalances", out var preBalances)) {
                            var pre = preBalances.EnumerateArray().FirstOrDefault(x => x.GetProperty("accountIndex").GetInt32() == accountIndex);
                            if (pre.ValueKind != JsonValueKind.Undefined)
                            {
                                var preUi = pre.GetProperty("uiTokenAmount");
                                decimal.TryParse(preUi.GetProperty("uiAmountString").GetString(), out preAmt);
                            }
                        }

                        decimal delta = postAmt - preAmt;
                        if (delta <= 0) continue;

                        using var ctx = _contextFactory.CreateDbContext();

                        // 3. Resolve Identity
                        if (!txResult.TryGetProperty("transaction", out var transactionObj)) continue;
                        if (!transactionObj.TryGetProperty("message", out var message)) continue;
                        if (!message.TryGetProperty("accountKeys", out var accountKeys)) continue;
                        
                        string feePayer = "";
                        if (accountKeys.ValueKind == JsonValueKind.Array) {
                            var first = accountKeys.EnumerateArray().FirstOrDefault();
                            feePayer = first.ValueKind == JsonValueKind.Object ? first.GetProperty("pubkey").GetString() : first.GetString();
                        }

                        string owner = "";
                        if (balance.TryGetProperty("owner", out var ownerNode)) owner = ownerNode.GetString();

                        // Match Player (Priority: Recipient Owner > Signer Fallback)
                        var wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.AddressType == "LUDC" && w.WalletAddress == owner);
                        bool matchedViaSigner = false;
                        if (wallet == null) {
                            wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.AddressType == "LUDC" && w.WalletAddress == feePayer);
                            matchedViaSigner = true;
                        }

                        // Treasury-origin transfers to external wallets should be ignored,
                        // but treasury-origin transfers to another registered player wallet
                        // are valid deposits and must still be credited.
                        var masterWallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == _masterUserId && w.AddressType == "LUDC");
                        bool isTreasurySource = false;
                        if (masterWallet != null && meta.TryGetProperty("preTokenBalances", out var preBalancesObj)) {
                            var treasuryPre = preBalancesObj.EnumerateArray().FirstOrDefault(b =>
                                b.TryGetProperty("owner", out var o) && o.GetString() == masterWallet.WalletAddress &&
                                b.GetProperty("mint").GetString() == LUDC_MINT.Key);

                            if (treasuryPre.ValueKind != JsonValueKind.Undefined) {
                                int tIdx = treasuryPre.GetProperty("accountIndex").GetInt32();
                                decimal tPre = decimal.Parse(treasuryPre.GetProperty("uiTokenAmount").GetProperty("uiAmountString").GetString());
                                decimal tPost = 0;
                                if (meta.TryGetProperty("postTokenBalances", out var postBalancesObj)) {
                                    var tPostMatch = postBalancesObj.EnumerateArray().FirstOrDefault(p => p.GetProperty("accountIndex").GetInt32() == tIdx);
                                    if (tPostMatch.ValueKind != JsonValueKind.Undefined)
                                        tPost = decimal.Parse(tPostMatch.GetProperty("uiTokenAmount").GetProperty("uiAmountString").GetString());
                                }
                                if (tPost < tPre) isTreasurySource = true;
                            }
                        }

                        if (isTreasurySource && (wallet == null || wallet.PlayerId == _masterUserId)) {
                            Console.WriteLine($"[LudcProvider] Skipping Treasury Payout to external/non-player wallet: {sig.Substring(0,8)}...");
                            continue;
                        }

                        if (wallet != null)
                        {
                            Console.WriteLine($"[LudcProvider] SUCCESS! Player {wallet.PlayerId} detected via {(matchedViaSigner ? "Signer" : "Owner")}. Amount: {delta}");
                            result.Deposits.Add(new TokenDeposit { Signature = sig, WalletAddress = wallet.WalletAddress, Amount = delta });
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"[LudcProvider] Error processing transaction {sig.Substring(0,8)}: {ex.Message}");
                }
            }
            return result;
        }
        private static readonly HttpClient _http = new HttpClient();
        
        private async Task<JsonDocument?> GetTransactionRaw(string signature)
        {
            if(!_http.DefaultRequestHeaders.Contains("User-Agent"))
                _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36");

            var request = new {
                jsonrpc = "2.0", id = 1, method = "getTransaction",
                @params = new object[] { signature, new { encoding = "jsonParsed", maxSupportedTransactionVersion = 0 } }
            };

            string primaryUrl = string.IsNullOrEmpty(rpcUrl) ? "https://api.mainnet-beta.solana.com" : rpcUrl;
            int totalWaitTime = 0;
            
            while (true) // Never abandon due to rate limits
            {
                try {
                    var response = await _http.PostAsJsonAsync(primaryUrl, request);
                    var json = await response.Content.ReadAsStringAsync();
                    var doc = JsonDocument.Parse(json);

                    if (doc.RootElement.TryGetProperty("result", out var res) && res.ValueKind != JsonValueKind.Null)
                        return doc;

                    // If rate limited, wait and retry indefinitely
                    if (json.Contains("429") || json.Contains("Too many requests")) {
                        int backoff = Math.Min(10, 3 + (totalWaitTime / 5000)); // Max 10s wait
                        Console.WriteLine($"[LudcProvider] Rate Limited on {signature.Substring(0,8)}. Waiting {backoff}s before retry...");
                        await Task.Delay(backoff * 1000);
                        totalWaitTime += (backoff * 1000);
                        continue;
                    }
                } catch { }

                // Fallback logic
                if (primaryUrl != "https://api.mainnet-beta.solana.com")
                {
                    try {
                        var fallbackRes = await _http.PostAsJsonAsync("https://api.mainnet-beta.solana.com", request);
                        var fallbackJson = await fallbackRes.Content.ReadAsStringAsync();
                        var fallbackDoc = JsonDocument.Parse(fallbackJson);

                        if (fallbackDoc.RootElement.TryGetProperty("result", out var fRes) && fRes.ValueKind != JsonValueKind.Null)
                            return fallbackDoc;

                        if (fallbackJson.Contains("429")) {
                            Console.WriteLine($"[LudcProvider] Public RPC Rate Limited on fallback. Waiting 5s...");
                            await Task.Delay(5000);
                            continue;
                        }
                    } catch { }
                }
                
                // If it's NOT a rate limit error but still null, then the transaction genuinely isn't found
                return null;
            }
        }
        public async Task<string> InitializeLatestSignature()
        {
            try
            {
                var sigs = await _rpc.GetSignaturesForAddressAsync(LUDC_MINT, limit: 1);

                if (sigs.WasSuccessful && sigs.Result?.Count > 0)
                {
                   string _lastProcessedSignature = sigs.Result[0].Signature;

                    Console.WriteLine($"Deposit scanner starting from: {_lastProcessedSignature}");
                    return _lastProcessedSignature;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LudcProvider] Network error during initialization: {ex.Message}");
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
    public class ScannerResult
    {
        public List<TokenDeposit> Deposits { get; set; } = new();
        public string LatestSeenSignature { get; set; } = "";
    }

    public class BroadcastResult
    {
        public bool WasSuccessful { get; set; }
        public string Result { get; set; } = "";
        public string Reason { get; set; } = "";
    }
}
