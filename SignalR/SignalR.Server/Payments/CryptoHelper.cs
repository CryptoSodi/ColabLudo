using LudoServer.Data;
using LudoServer.Models;
using Microsoft.EntityFrameworkCore;

namespace SignalR.Server.Payments
{
    public class CryptoHelper(IDbContextFactory<LudoDbContext> _contextFactory, PaymentProviderFactory _factory)
    {
        public async Task<PlayerWallet> EnsurePlayerWalletExists(int playerId, CurrencyType currencyType)
        {
            return await _factory.Get(currencyType).EnsurePlayerWalletExists(playerId, currencyType.ToString());
        }
        public async Task<bool> deductGameFee(int playerId, int? tournamentId, string roomCode, bool isTournamentGame, decimal betAmount, int retryCount = 0)
        {
            var provider = _factory.Get(CurrencyType.LUDC);
            if (betAmount <= 0)
                return false;

            using var ctx = _contextFactory.CreateDbContext();
            using var tx = await ctx.Database.BeginTransactionAsync();

            var wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(p => p.PlayerId == playerId);
            if (wallet == null || wallet.IsWithdrawalLocked || wallet.AvailableBalance < betAmount)
                return false;

            // ❗ Block game actions during withdrawal
            if (wallet.IsWithdrawalLocked)
                return false;

            if (wallet.AvailableBalance < betAmount)
                return false;

            if (isTournamentGame)
            {
                var challenger = await ctx.TournamentChallengers.FirstOrDefaultAsync(tc => tc.TournamentId == tournamentId && tc.PlayerId == playerId);

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
                    // 💰 Deduct tournament fee (OFF-CHAIN, ATOMIC)
                    await ApplyOffChainLedger(ctx, wallet, -betAmount, $"Tournament Fee {challenger.RetryCount}", tournamentId?.ToString() ?? "", false, roomCode);

                }
                else
                {
                    // Already joined & paid
                    await tx.CommitAsync();
                    return true;
                }
            }
            else
            {
                // 💰 Deduct normal game fee
              await  ApplyOffChainLedger(ctx, wallet, -betAmount, "Game Fee", "", false, roomCode);
            }

            await ctx.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        }
        public async Task<bool> OffChainTransaction(int playerId, decimal amount, string description, string txId = "", bool isOnChain = false, string roomCode = "", TransactionType type = TransactionType.Deposit)
        {
            using var ctx = _contextFactory.CreateDbContext();
            using var tx = await ctx.Database.BeginTransactionAsync();
            var wallet = await EnsurePlayerWalletExists(playerId, CurrencyType.LUDC);
            // Block during withdrawal
            if (wallet.IsWithdrawalLocked)
                return false;

            await ApplyOffChainLedger(ctx, wallet, amount, description, txId, isOnChain, roomCode, type);

            await ctx.SaveChangesAsync();
            await tx.CommitAsync(); // ✅ Now this works
            return true;
        }
        // =========================
        // OFF-CHAIN LEDGER
        // =========================
        public async Task<bool> ApplyOffChainLedger(LudoDbContext ctx, PlayerWallet wallet, decimal amount, string description, string txId = "", bool isOnChain = false, string roomCode = "", TransactionType? type = null)
        {
            wallet.AvailableBalance += amount;

            var finalType = type ?? (amount >= 0 ? TransactionType.Deposit : TransactionType.Sweep);

            ctx.WalletTransaction.Add(new WalletTransaction
            {
                PlayerId = wallet.PlayerId,
                OperationId = Guid.NewGuid(),
                Amount = amount,
                BalanceAfter = wallet.AvailableBalance,
                Type = finalType,
                Status = WalletTransactionStatus.Completed,
                Description = description,
                RoomCode = roomCode,
                IsOnChain = isOnChain,
                txId = txId,
                AddressType = "LUDC"
            });

            ctx.Update(wallet);

            return true;
        }
        internal string Withdraw(Player player, string destination, decimal amount)
        {
            var operationId = Guid.NewGuid();
            var provider = _factory.Get(CurrencyType.LUDC);
            return provider.WithdrawAsync(player, destination, amount, operationId).GetAwaiter().GetResult();
        }
        public async Task<string> MintNFT(int playerid, int amount)
        {
            var provider = _factory.Get(CurrencyType.LUDC);
            return "success";
          //  return provider.MintNFT(playerid, amount);
        }
    }
}