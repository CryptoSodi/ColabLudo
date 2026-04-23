using Ludo.Api.Services;
using Microsoft.AspNetCore.Mvc;
using SharedCode.Constants;
using SignalR.Server.Services;

namespace Ludo.Api.Controllers;

[ApiController]
[Route("api")]
public class ProfileController(
    ApiPlayerContext playerContext,
    UtilService utilService) : ControllerBase
{
    [HttpGet("profile")]
    public async Task<ActionResult<PlayerInfo>> GetProfile()
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
            return Unauthorized();

        return await utilService.CastPlayerToInfoAsync(player);
    }

    [HttpGet("wallet")]
    public async Task<ActionResult<PlayerWallet>> GetWallet()
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
            return Unauthorized();

        var info = await utilService.CastPlayerToInfoAsync(player);
        if (info.Wallet == null)
            return NotFound();

        return info.Wallet;
    }
}
