using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SharedCode.Constants;
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
                var player = await _googleAuthService.GoogleAuthentication(idToken, city, countryCode, "Admin");
                ConnectionToPlayer[Context.ConnectionId] = await _utilService.GetPlayerByID(player.PlayerId);
                await _utilService.SetPlayerOnlineState(player.PlayerId, true);
                return await _utilService.CastPlayerToInfoAsync(player);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Authentication : {ex.Message} ");
                // If player creation failed, return null
                return null;
            }
        }
    }
}