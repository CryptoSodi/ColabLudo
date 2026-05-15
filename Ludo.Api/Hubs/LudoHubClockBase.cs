using Ludo.Api.Services;
using Microsoft.AspNetCore.SignalR;
using SignalR.Server;
using System.Collections.Concurrent;

namespace Ludo.Api.Hubs;

public abstract class LudoHubClockBase(ApiPlayerContext playerContext, DatabaseManager databaseManager, IHubContext<LudoHub> hubContext) : LudoHubBase(playerContext, databaseManager, hubContext)
{
    private const int ServerClockPingIntervalMs = 200;
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> ClockPingTokens = new();

    public override async Task OnConnectedAsync()
    {
        var token = GetAuthToken();
        Console.WriteLine($"[LudoHub] Connected. ConnectionId={Context.ConnectionId}, HasAuthToken={!string.IsNullOrWhiteSpace(token)}");
        StartClockPingLoopForConnection(Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        StopClockPingLoopForConnection(Context.ConnectionId);
        Console.WriteLine($"[LudoHub] Disconnected. ConnectionId={Context.ConnectionId}, Error={exception?.Message ?? "none"}");
        await base.OnDisconnectedAsync(exception);
    }

    private void StartClockPingLoopForConnection(string connectionId)
    {
        StopClockPingLoopForConnection(connectionId);

        var cts = new CancellationTokenSource();
        ClockPingTokens[connectionId] = cts;
        var token = cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var serverTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    await HubContext.Clients.Client(connectionId).SendAsync("ReceiveServerClockPing", serverTimeMs, token);
                    await Task.Delay(ServerClockPingIntervalMs, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LudoHub] Clock ping loop error. ConnectionId={connectionId}, Error={ex.Message}");
            }
        }, token);
    }

    private static void StopClockPingLoopForConnection(string connectionId)
    {
        if (!ClockPingTokens.TryRemove(connectionId, out var cts))
            return;

        try
        {
            cts.Cancel();
        }
        catch
        {
        }
        finally
        {
            cts.Dispose();
        }
    }
}
