using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SharedCode.Constants;

namespace SignalR.Server.Services
{
    public class UtilService(IDbContextFactory<LudoDbContext> _contextFactory, CryptoHelper _crypto)
    {
        public async Task<PlayerInfo> CastPlayerToInfoAsync(Player player)
        {
            var pw = await _crypto.EnsurePlayerWalletExists(player.PlayerId);

            return new PlayerInfo
            {
                PlayerId = player.PlayerId,
                Name = player.Name,
                Email = player.Email,
                PictureUrl = player.PictureUrl,
                PhoneNumber = player.PhoneNumber,
                City = player.City,
                CountryCode = player.CountryCode,
                IsOnline = player.IsOnline,
                AuthToken = player.AuthToken,
                GamesPlayed = player.GamesPlayed,
                GamesWon = player.GamesWon,
                GamesLost = player.GamesLost,
                BestWin = player.BestWin,
                TotalWin = player.TotalWin,
                TotalLost = player.TotalLost,
                Score = player.Score,
                Role = player.Role,
                Wallet = new SharedCode.Constants.PlayerWallet
                {
                    PlayerId = pw.PlayerId,
                    AddressType = pw.AddressType,
                    WalletAddress = pw.WalletAddress,
                    AvailableBalance = pw.AvailableBalance,
                    SignupBonus = 10,
                    Transactions = getTransactions(player.PlayerId)
                }
            };
        }

        private ICollection<SharedCode.Constants.WalletTransaction> getTransactions(int playerId)
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                List<SharedCode.Constants.WalletTransaction> transactions = ctx.WalletTransaction.Where(p => p.PlayerId == playerId).Select(t => new SharedCode.Constants.WalletTransaction
                {
                    TransactionId = t.TransactionId,
                    txId = t.txId,
                    PlayerId = t.PlayerId,
                    CreatedDate = t.CreatedDate,
                    Amount = t.Amount,
                    BalanceAfter = t.BalanceAfter,
                    Type = (SharedCode.Constants.TransactionType)t.Type,
                    Description = t.Description,
                    IsOnChain = t.IsOnChain,
                    RoomCode = t.RoomCode
                })
            .ToList();
                return transactions;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating online status for player {playerId}: {ex.Message}");
            }
            return new List<SharedCode.Constants.WalletTransaction>();
        }

        public async Task SetPlayerOnlineState(int playerId, bool isOnline)
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                var player = new Player { PlayerId = playerId, IsOnline = isOnline };
                ctx.Players.Attach(player);
                ctx.Entry(player).Property(p => p.IsOnline).IsModified = true;
                await ctx.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating online status for player {playerId}: {ex.Message}");
            }
        }
        public async Task<Player> GetPlayerByID(int PlayerId)
        {
            using var ctx = _contextFactory.CreateDbContext();
            Player sender = ctx.Players.Find(PlayerId);
            if (sender == null)
                throw new HubException("Player not recognized.");

            var wal = await _crypto.EnsurePlayerWalletExists(PlayerId);
            if (wal == null)
                throw new HubException("Player Wallet not Found.");

            sender.Wallets = new List<LudoServer.Models.PlayerWallet>
                {
                    new LudoServer.Models.PlayerWallet
                    {
                        PlayerId = sender.PlayerId,
                        AddressType = wal.AddressType,
                        WalletAddress = wal.WalletAddress,
                        AvailableBalance = wal.AvailableBalance
                    }
                };
            return sender;
        }
    }
}