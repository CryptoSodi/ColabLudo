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
        public async Task<bool> deductGameFee(int playerId, int? tournamentId, string roomCode, bool isTournamentGame, decimal betAmount)
        {
            var provider = _factory.Get(CurrencyType.LUDC);
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
                }
                else
                {
                    // Already joined & paid
                    await tx.CommitAsync();
                    return true;
                }

                // 💰 Deduct tournament fee (OFF-CHAIN, ATOMIC)
               await ApplyOffChainLedger(ctx, wallet, -betAmount, "Tournament Fee", tournamentId?.ToString() ?? "", false, roomCode);
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
        public async Task<bool> OffChainTransaction(int playerId, decimal amount, string description, string txId = "", bool isOnChain = false, string roomCode = "")
        {
            using var ctx = _contextFactory.CreateDbContext();
            using var tx = await ctx.Database.BeginTransactionAsync();
            var wallet = await EnsurePlayerWalletExists(playerId, CurrencyType.LUDC);
            // Block during withdrawal
            if (wallet.IsWithdrawalLocked)
                return false;

            await ApplyOffChainLedger(ctx, wallet, amount, description, roomCode, isOnChain, txId);

            await ctx.SaveChangesAsync();
            await tx.CommitAsync(); // ✅ Now this works
            return true;
        }
        // =========================
        // OFF-CHAIN LEDGER
        // =========================
        public async Task<bool> ApplyOffChainLedger(LudoDbContext ctx, PlayerWallet wallet, decimal amount, string description, string txId = "", bool isOnChain = false, string roomCode = "")
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
                txId = txId,
                AddressType = "LUDC"
            });

            ctx.Update(wallet);

            return true;
        }
        internal string Withdraw(Player player, string destination, decimal amountInSol)
        {
            var operationId = Guid.NewGuid();
            var provider = _factory.Get(CurrencyType.LUDC);
            return provider.WithdrawAsync(player, destination, amountInSol, operationId).GetAwaiter().GetResult();
        }
        public async Task<string> MintNFT(int playerid, int amount)
        {
            var provider = _factory.Get(CurrencyType.LUDC);
            return "success";
          //  return provider.MintNFT(playerid, amount);
        }
    }
}