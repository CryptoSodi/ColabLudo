using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SharedCode.Constants;
using SignalR.Server.Payments;
using SignalR.Server.Services;
using System.Collections.Concurrent;

namespace SignalR.Server
{
    public class AdminHub(IDbContextFactory<LudoDbContext> _contextFactory, DatabaseManager DM, CryptoHelper _crypto, FriendsService _friendsService, TournamentService _tournamentService, DailyBonusService _dailyBonusService, GoogleAuthService _googleAuthService, UtilService _utilService) : Hub
    {
        public static ConcurrentDictionary<string, Player> ConnectionToPlayer = new ConcurrentDictionary<string, Player>();

        public override async Task OnConnectedAsync()
        {
            try
            {
                Console.WriteLine($"Admin User connected: {Context.ConnectionId}");             
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnConnectedAsync: {ex.Message}");
            }
            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnDisconnectedAsync: {ex.Message}");
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task<PlayerInfo> GoogleAuthentication(string idToken, string city, string countryCode)
        {
            try
            {
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Authentication : {ex.Message}");
            }
                return null;
        }
        public async Task<PlayerInfo> UserConnectedSetID(String AuthToken)
        {
            // 1) Store SignalR connection
            try
            {
                Console.WriteLine($"Ping {Context.ConnectionId}");
                ConnectionToPlayer[Context.ConnectionId] = await _utilService.GetPlayerByID(int.Parse(_utilService.Decrypt(AuthToken)));
                return await _utilService.CastPlayerToInfoAsync(ConnectionToPlayer[Context.ConnectionId]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in {AuthToken} : UserConnectedSetID: {ex.Message}");
            }
            return null;
        }
        public async Task<List<Game>> GetGame(String AuthToken, bool IsPrivate)
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                Player player = await _utilService.GetPlayerByID(int.Parse(_utilService.Decrypt(AuthToken)));
                //g.State == "Active"
                return await ctx.Games.Where(g => g.MultiPlayer.P1 == player.PlayerId || g.MultiPlayer.P2 == player.PlayerId || g.MultiPlayer.P3 == player.PlayerId || g.MultiPlayer.P4 == player.PlayerId && g.State == "Completed").ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetGame: {ex.Message}");
                return new List<Game>();
            }
        }
    }
}