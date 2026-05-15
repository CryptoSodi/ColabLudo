using Ludo.Api.Services;
using LudoServer.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SignalR.Server;
using SignalR.Server.Services;

namespace Ludo.Api.Hubs;

public abstract class LudoHubDailyBonusBase(
    ApiPlayerContext playerContext,
    DatabaseManager databaseManager,
    IHubContext<LudoHub> hubContext,
    IDbContextFactory<LudoDbContext> contextFactory,
    DailyBonusService dailyBonusService) : LudoHubChatBase(playerContext, databaseManager, hubContext, contextFactory)
{
    private readonly DailyBonusService _dailyBonusService = dailyBonusService;

    public async Task<DailyBonusDto?> GetDailyBonus()
    {
        var token = GetAuthToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            Console.WriteLine("[DailyBonusHub] Get unauthorized. Reason=MissingAuthToken");
            return null;
        }

        var authContext = new DefaultHttpContext();
        authContext.Request.Headers["X-Auth-Token"] = token;
        var player = await PlayerContext.GetAuthenticatedPlayerAsync(authContext.Request);
        if (player == null)
        {
            Console.WriteLine("[DailyBonusHub] Get unauthorized. Reason=InvalidAuthToken");
            return null;
        }

        Console.WriteLine($"[DailyBonusHub] Get requested. PlayerId={player.PlayerId}");
        var dto = await _dailyBonusService.GetDailyBonus(player);
        Console.WriteLine($"[DailyBonusHub] Get completed. PlayerId={player.PlayerId}");
        return dto;
    }

    public async Task<DailyBonusDto?> ClaimTodayBonus()
    {
        var token = GetAuthToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            Console.WriteLine("[DailyBonusHub] Claim unauthorized. Reason=MissingAuthToken");
            return null;
        }

        var authContext = new DefaultHttpContext();
        authContext.Request.Headers["X-Auth-Token"] = token;
        var player = await PlayerContext.GetAuthenticatedPlayerAsync(authContext.Request);
        if (player == null)
        {
            Console.WriteLine("[DailyBonusHub] Claim unauthorized. Reason=InvalidAuthToken");
            return null;
        }

        Console.WriteLine($"[DailyBonusHub] Claim requested. PlayerId={player.PlayerId}");
        var dto = await _dailyBonusService.ClaimTodayBonus(player);
        Console.WriteLine($"[DailyBonusHub] Claim completed. PlayerId={player.PlayerId}");
        return dto;
    }
}
