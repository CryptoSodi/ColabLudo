using Ludo.Api.Services;
using LudoServer.Data;
using Microsoft.AspNetCore.Mvc;
using SignalR.Server.Services;

namespace Ludo.Api.Controllers;

[ApiController]
[Route("api/daily-bonus")]
public class DailyBonusController(
    ApiPlayerContext playerContext,
    DailyBonusService dailyBonusService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DailyBonusDto>> GetDailyBonus()
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
        {
            Console.WriteLine("[DailyBonusApi] Get unauthorized.");
            return Unauthorized();
        }

        Console.WriteLine($"[DailyBonusApi] Get requested. PlayerId={player.PlayerId}");
        var dto = await dailyBonusService.GetDailyBonus(player);
        Console.WriteLine($"[DailyBonusApi] Get completed. PlayerId={player.PlayerId}");
        return dto;
    }

    [HttpPost("claim")]
    public async Task<ActionResult<DailyBonusDto>> ClaimTodayBonus()
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
        {
            Console.WriteLine("[DailyBonusApi] Claim unauthorized.");
            return Unauthorized();
        }

        Console.WriteLine($"[DailyBonusApi] Claim requested. PlayerId={player.PlayerId}");
        var dto = await dailyBonusService.ClaimTodayBonus(player);
        Console.WriteLine($"[DailyBonusApi] Claim completed. PlayerId={player.PlayerId}");
        return dto;
    }
}
