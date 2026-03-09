using Microsoft.AspNetCore.SignalR;

namespace SignalR.Server.Services
{
    public class PlayerCleanupService : BackgroundService
    {
        private readonly DatabaseManager _dm;
        private readonly IHubContext<LudoHub> _hubContext;

        public PlayerCleanupService(DatabaseManager dm, IHubContext<LudoHub> hubContext)
        {
            _dm = dm;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("🔥 PlayerCleanupService STARTED");
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                Console.WriteLine($"Ping check : {now} : Count {LudoHub.ConnectionToPlayer.ToList().Count()}");

                foreach (var kv in LudoHub.ConnectionToPlayer.ToList())
                {
                    var connectionId = kv.Key;
                    var player = kv.Value;

                    if (now - player.LastPingUtc > TimeSpan.FromSeconds(30))
                    {
                        Console.WriteLine($"Force removing inactive player: {player.PlayerId}");

                        LudoHub.ConnectionToPlayer.TryRemove(connectionId, out _);

                        var (existingGame, user) = await _dm.LeaveGameLobby(player.PlayerId);

                        if (existingGame != null)
                        {
                            await _hubContext.Clients.Group(existingGame.RoomCode)
                                .SendAsync("PlayerSeatUpdateNeeded");
                        }
                    }
                }

                await Task.Delay(15000, stoppingToken);
            }
        }
    }
}
