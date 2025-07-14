using LudoServer.Data;
using LudoServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using SharedCode.Constants;

namespace SignalR.Server.Services
{
    public class UtilService
    {
        private readonly IDbContextFactory<LudoDbContext> _contextFactory;
        private readonly CryptoHelper _crypto;
        public UtilService(IDbContextFactory<LudoDbContext> contextFactory, CryptoHelper crypto)
        {
            _contextFactory = contextFactory;
            _crypto = crypto;
        }
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
                Wallet = new SharedCode.Constants.PlayerWallet
                {
                    PlayerId = pw.PlayerId,
                    AddressType = pw.AddressType,
                    WalletAddress = pw.WalletAddress,
                    AvailableBalance = pw.AvailableBalance,
                    SignupBonus = 10
                }
            };
        }
        public async Task SetPlayerOnlineState(int playerId, bool isOnline)
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                var player = await ctx.Players.FirstOrDefaultAsync(p => p.PlayerId == playerId);
                if (player != null)
                {
                    player.IsOnline = isOnline;
                    await ctx.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating online status for player {playerId}: {ex.Message}");
            }
        }
    }
}
