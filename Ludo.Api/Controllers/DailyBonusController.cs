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
            return Unauthorized();

        return await dailyBonusService.GetDailyBonus(player);
    }

    [HttpPost("claim")]
    public async Task<ActionResult<DailyBonusDto>> ClaimTodayBonus()
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
            return Unauthorized();

        return await dailyBonusService.ClaimTodayBonus(player);
    }
}
