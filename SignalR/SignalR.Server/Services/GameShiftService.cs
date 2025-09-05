using LudoServer.Data;
using LudoServer.Models;
using Microsoft.EntityFrameworkCore;
using SharedCode.Constants;
using System.Text;
using System.Text.Json;

namespace SignalR.Server.Services
{
    public class GameShiftService(IDbContextFactory<LudoDbContext> contextFactory, HttpClient httpClient)
    {
        private readonly string _apiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJrZXkiOiJkNzg4MmRjNS0zN2Q0LTRhZDktOTRlNy0yODQyOWQxYzYxMjYiLCJzdWIiOiJhZjMwOWEwYy0zZWM3LTRlOGYtODQzOC0wNzE3Y2U4NjgwNTEiLCJpYXQiOjE3NTY5MzAxNDV9._MLa0bDR7SbfwguPXOgqNGQjE7nwLo_XW8o-45dFH_Q";

        public void CreateUserAsync(PlayerInfo playerInfo)
        {
            var url = "https://api.gameshift.dev/nx/users";

            var body = new
            {
                referenceId = playerInfo?.AuthToken?.ToString(), // unique reference from your system
                email = playerInfo?.Email,
                externalWalletAddress = playerInfo?.Wallet?.WalletAddress, // optional
            };

            var json = JsonSerializer.Serialize(body);
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("accept", "application/json");
            request.Headers.Add("x-api-key", _apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response =  httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
            }
        }
    }
}
