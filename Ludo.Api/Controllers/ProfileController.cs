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
        {
            Console.WriteLine("[ProfileApi] GetProfile unauthorized.");
            return Unauthorized();
        }

        Console.WriteLine($"[ProfileApi] GetProfile requested. PlayerId={player.PlayerId}");
        var info = await utilService.CastPlayerToInfoAsync(player);
        Console.WriteLine($"[ProfileApi] GetProfile completed. PlayerId={player.PlayerId}");
        return info;
    }

    [HttpGet("wallet")]
    public async Task<ActionResult<PlayerWallet>> GetWallet()
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
        {
            Console.WriteLine("[ProfileApi] GetWallet unauthorized.");
            return Unauthorized();
        }

        Console.WriteLine($"[ProfileApi] GetWallet requested. PlayerId={player.PlayerId}");
        var info = await utilService.CastPlayerToInfoAsync(player);
        if (info.Wallet == null)
        {
            Console.WriteLine($"[ProfileApi] GetWallet not found. PlayerId={player.PlayerId}");
            return NotFound();
        }

        Console.WriteLine($"[ProfileApi] GetWallet completed. PlayerId={player.PlayerId}, Balance={info.Wallet.AvailableBalance}");
        return info.Wallet;
    }
}
