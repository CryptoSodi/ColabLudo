using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Types;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;

namespace SignalR.Server
{
    public class SolCryptoHelper
    {
        // Number of lamports in one SOL.
        private const ulong LamportsPerSol = 1_000_000_000;
        private readonly IRpcClient _rpc;                                   // Solana RPC client
        private readonly IDbContextFactory<LudoDbContext> _contextFactory;  // EF factory for ledger DB
        private readonly IDataProtector _protector;                         // Data protector for encrypt/decrypt
        private readonly int _masterUserId;                                 // Identifier for the master wallet
        private async Task<ulong> getFeeBuffer()
        {
            // 1) Optional: dynamically get accurate fee estimate
            /*
            var message = new TransactionBuilder()
                .SetFeePayer(new PublicKey(pub))
                .AddInstruction(SystemProgram.Transfer(...))
                .BuildMessage();
            var feeEstimate = await _rpc.GetFeeForMessageAsync(Convert.ToBase64String(message));
            if (feeEstimate.Value > feeBuffer)
                feeBuffer = feeEstimate.Value;
            */
            //  Console.WriteLine("Fetching fee buffer..."+ (await _rpc.GetMinimumBalanceForRentExemptionAsync(0)).Result * 2);
            return (await _rpc.GetMinimumBalanceForRentExemptionAsync(0)).Result * 2;
        }
        public SolCryptoHelper(IDbContextFactory<LudoDbContext> contextFactory, IHostEnvironment env, IDataProtectionProvider dataProtectionProvider, int masterUserId, string network, string purpose)
        {
            _contextFactory = contextFactory;            
            _protector = dataProtectionProvider.CreateProtector(purpose);
            _masterUserId = masterUserId;
            // Initialize Solana RPC for MainNet or DevNet
            // Constructor sets up RPC client, loads or creates wallets, and ensures
            var cluster = network.Equals("DevNet", StringComparison.OrdinalIgnoreCase) ? Cluster.DevNet : Cluster.MainNet;
            _rpc = ClientFactory.GetClient(cluster);
            EnsureWalletAsync(_masterUserId).GetAwaiter().GetResult();            
        }
        /* =========================================================
       * WALLET INITIALIZATION (SINGLE SOURCE OF TRUTH)
       * ========================================================= */
        public async Task<PlayerWallet> EnsureWalletAsync(int playerId, bool applySignupBonus = false)
        {
            using var ctx = _contextFactory.CreateDbContext();
            using var tx = await ctx.Database.BeginTransactionAsync();

            bool isMaster = playerId == _masterUserId;

            // =========================================================
            // 1️⃣ ENSURE PLAYER EXISTS (NORMAL OR MASTER)
            // =========================================================
            var player = await ctx.Players.FirstOrDefaultAsync(p => p.PlayerId == playerId);
            if (player == null && isMaster)
            {
                player = new Player
                {
                    CreatedDate = DateTime.UtcNow,
                    Name = isMaster ? "SYSTEM" : $"Player_{playerId}"
                };

                ctx.Players.Add(player);
                await ctx.SaveChangesAsync(); // FK safety
            }

            // ---- Wallet Key ----
            var walletKey = await ctx.PlayerWalletKey.FirstOrDefaultAsync(w => w.PlayerId == playerId);

            if (walletKey == null)
            {
                var acct = new Account();
                var rawPriv = new Base58Encoder().EncodeData(acct.PrivateKey);

                walletKey = new PlayerWalletKey
                {
                    PlayerId = playerId,
                    PublicKey = acct.PublicKey.Key,
                    EncryptedPrivateKey = _protector.Protect(rawPriv),
                    IsMaster = isMaster
                };

                ctx.PlayerWalletKey.Add(walletKey);
                await ctx.SaveChangesAsync();
            }

            // ---- Player Wallet ----
            var wallet = await ctx.PlayerWallet
                .FirstOrDefaultAsync(w => w.PlayerId == playerId);

            if (wallet == null)
            {
                wallet = new PlayerWallet
                {
                    PlayerId = playerId,
                    AddressType = "LUDC",
                    WalletAddress = walletKey.PublicKey,
                    AvailableBalance = 0m
                };

                ctx.PlayerWallet.Add(wallet);
                await ctx.SaveChangesAsync();

                if (applySignupBonus && !isMaster)
                {
                    ApplyOffChainLedger(
                        ctx,
                        wallet,
                        10000m,
                        "Signup Bonus"
                    );

                    await ctx.SaveChangesAsync();
                }
            }

            await tx.CommitAsync();
            return wallet;
        }

        /* =========================================================
         * INTERNAL LEDGER WRITER (NO DB CONTEXT / NO COMMIT)
         * ========================================================= */

        public void ApplyOffChainLedger(LudoDbContext ctx, PlayerWallet wallet, decimal amount, string description, string roomCode = "", bool isOnChain = false, string txId = "")
        {
            wallet.AvailableBalance += amount;

            ctx.WalletTransaction.Add(new WalletTransaction
            {
                PlayerId = wallet.PlayerId,
                OperationId = Guid.NewGuid(),
                Amount = amount,
                BalanceAfter = wallet.AvailableBalance,
                Type = amount >= 0 ? TransactionType.Deposit : TransactionType.Sweep,
                Status = WalletTransactionStatus.Completed,
                Description = description,
                RoomCode = roomCode,
                IsOnChain = isOnChain,
                txId = txId
            });

            ctx.PlayerWallet.Update(wallet);
        }
        /* =========================================================
          * WITHDRAW (IDEMPOTENT, ADMIN FALLBACK)
          * ========================================================= */

        public async Task<string> Withdraw(Player player, string destination, decimal amount, Guid operationId)
        {
            using var ctx = _contextFactory.CreateDbContext();

            var existing = await ctx.WalletTransaction.FirstOrDefaultAsync(t => t.OperationId == operationId);

            if (existing != null)
            {
                if (existing.Status == WalletTransactionStatus.Completed)
                    return existing.txId;

                if (existing.Status == WalletTransactionStatus.Pending)
                    return "WITHDRAW_PENDING";
            }

            using var tx = await ctx.Database.BeginTransactionAsync();

            var wallet = await ctx.PlayerWallet.FirstAsync(p => p.PlayerId == player.PlayerId);

            if (wallet.IsWithdrawalLocked || wallet.AvailableBalance < amount)
                return "WITHDRAW_NOT_ALLOWED";

            wallet.AvailableBalance -= amount;
            wallet.IsWithdrawalLocked = true;

            var ledger = new WalletTransaction
            {
                PlayerId = player.PlayerId,
                OperationId = operationId,
                Amount = -amount,
                BalanceAfter = wallet.AvailableBalance,
                Type = TransactionType.Withdrawal,
                Status = WalletTransactionStatus.Pending,
                Description = "Withdraw Pending"
            };

            ctx.PlayerWallet.Update(wallet);
            ctx.WalletTransaction.Add(ledger);

            await ctx.SaveChangesAsync();
            await tx.CommitAsync();

            try
            {
                string sig;
                ulong feeBuffer = (await _rpc.GetMinimumBalanceForRentExemptionAsync(0)).Result * 2;

                var userKey = await GetWalletKeyAsync(player.PlayerId);
                var userBal = (await _rpc.GetBalanceAsync(userKey.PublicKey)).Result.Value;

                if ((userBal - feeBuffer) / (decimal)LamportsPerSol >= amount)
                    sig = await SendRawAsync(userKey, destination, amount);
                else
                    sig = await SendRawAsync(await GetWalletKeyAsync(_masterUserId), destination, amount);

                wallet.IsWithdrawalLocked = false;
                ledger.Status = WalletTransactionStatus.Completed;
                ledger.IsOnChain = true;
                ledger.txId = sig;
                ledger.Description = "Withdraw";

                ctx.PlayerWallet.Update(wallet);
                ctx.WalletTransaction.Update(ledger);
                await ctx.SaveChangesAsync();

                return sig;
            }
            catch
            {
                wallet.AvailableBalance += amount;
                wallet.IsWithdrawalLocked = false;

                ledger.Status = WalletTransactionStatus.Failed;
                ledger.Description = "Withdraw Failed";

                ctx.PlayerWallet.Update(wallet);
                ctx.WalletTransaction.Update(ledger);
                await ctx.SaveChangesAsync();

                return "WITHDRAW_FAILED";
            }
        }
        /* =========================================================
          * RAW SOLANA SEND
          * ========================================================= */
        private async Task<string> SendRawAsync(PlayerWalletKey key, string to, decimal amount)
        {
            var rawPriv = _protector.Unprotect(key.EncryptedPrivateKey);
            var acct = new Account(rawPriv, key.PublicKey);

            var lamports = (ulong)(amount * LamportsPerSol);
            var blockhash = (await _rpc.GetLatestBlockHashAsync()).Result.Value.Blockhash;

            var tx = new TransactionBuilder()
                .SetRecentBlockHash(blockhash)
                .SetFeePayer(acct.PublicKey)
                .AddInstruction(SystemProgram.Transfer(
                    acct.PublicKey,
                    new PublicKey(to),
                    lamports)).Build(acct);

            var res = await _rpc.SendTransactionAsync(tx, false, Commitment.Confirmed);
            if (!res.WasSuccessful)
                throw new Exception(res.Reason);

            return res.Result;
        }
        private async Task<PlayerWalletKey> GetWalletKeyAsync(int playerId)
        {
            using var ctx = _contextFactory.CreateDbContext();
            return await ctx.PlayerWalletKey.FirstAsync(w => w.PlayerId == playerId);
        }
        public async Task<bool> DeductGameFee(int playerId,int? tournamentId,string roomCode,bool isTournamentGame,decimal betAmount)
        {
            if (betAmount <= 0)
                return false;

            using var ctx = _contextFactory.CreateDbContext();
            using var tx = await ctx.Database.BeginTransactionAsync();

            var wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(p => p.PlayerId == playerId);
            if (wallet == null)
                return false;

            // ❗ Block game actions during withdrawal
            if (wallet.IsWithdrawalLocked)
                return false;

            if (wallet.AvailableBalance < betAmount)
                return false;

            if (isTournamentGame)
            {
                var challenger = await ctx.TournamentChallengers.FirstOrDefaultAsync(tc =>tc.TournamentId == tournamentId &&tc.PlayerId == playerId);

                if (challenger == null)
                {
                    challenger = new TournamentChallenger
                    {
                        PlayerId = playerId,
                        TournamentId = tournamentId,
                        Status = "JOINED",
                        RetryCount = 0
                    };

                    ctx.TournamentChallengers.Add(challenger);
                }
                else if (challenger.Status == "FAILED")
                {
                    challenger.RetryCount++;
                    challenger.Status = "JOINED";
                    ctx.TournamentChallengers.Update(challenger);
                }
                else
                {
                    // Already joined & paid
                    await tx.CommitAsync();
                    return true;
                }

                // 💰 Deduct tournament fee (OFF-CHAIN, ATOMIC)
                ApplyOffChainLedger(ctx,wallet,-betAmount,"Tournament Fee",tournamentId?.ToString() ?? "",false,roomCode);
            }
            else
            {
                // 💰 Deduct normal game fee
                ApplyOffChainLedger(ctx,wallet,-betAmount,"Game Fee","",false,roomCode);
            }

            await ctx.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        }

        // Builds, signs, and sends a SOL transfer transaction on-chain.
        internal async Task<string> MintNFT(int playerid, int amount)
        {
            throw new NotImplementedException();
        }
    }
}