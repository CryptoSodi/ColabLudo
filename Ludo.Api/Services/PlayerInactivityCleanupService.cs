using SignalR.Server;
using SignalR.Server.Services;

namespace Ludo.Api.Services;

public class PlayerInactivityCleanupService(
    PlayerPresenceTracker presenceTracker,
    UtilService utilService,
    DatabaseManager databaseManager) : BackgroundService
{
    private static readonly TimeSpan InactivityTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var inactivePlayerIds = presenceTracker.GetInactivePlayerIds(InactivityTimeout, now);

                foreach (var playerId in inactivePlayerIds)
                {
                    await utilService.SetPlayerOnlineState(playerId, false, touchLastLogin: true);
                    await databaseManager.LeaveGameLobby(playerId);
                    presenceTracker.RemovePlayer(playerId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PresenceCleanup] Error: {ex.Message}");
            }

            await Task.Delay(SweepInterval, stoppingToken);
        }
    }
}
