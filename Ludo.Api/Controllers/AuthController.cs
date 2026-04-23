using Ludo.Api.Services;
using Microsoft.AspNetCore.Mvc;
using SharedCode.Constants;
using SignalR.Server.Services;

namespace Ludo.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    GoogleAuthService googleAuthService,
    UtilService utilService,
    ApiPlayerContext playerContext) : ControllerBase
{
    [HttpPost("google")]
    public async Task<ActionResult<PlayerInfo>> GoogleLogin([FromBody] GoogleLoginDto request)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            Console.WriteLine("[AuthApi] GoogleLogin rejected. Empty id token.");
            return BadRequest();
        }

        try
        {
            Console.WriteLine($"[AuthApi] GoogleLogin requested. City={request.City ?? "none"}, CountryCode={request.CountryCode ?? "none"}, IsGuest={request.IdToken.StartsWith("Guest", StringComparison.OrdinalIgnoreCase)}");
            var player = await googleAuthService.GoogleAuthentication(
                request.IdToken,
                request.City ?? "none",
                request.CountryCode ?? "none");

            if (player == null)
            {
                Console.WriteLine("[AuthApi] GoogleLogin unauthorized. Player not resolved.");
                return Unauthorized();
            }

            if (player.IsBlocked)
            {
                Console.WriteLine($"[AuthApi] GoogleLogin blocked. PlayerId={player.PlayerId}");
                return StatusCode(StatusCodes.Status423Locked, "ACCOUNT_BLOCKED");
            }

            await utilService.SetPlayerOnlineState(player.PlayerId, true);
            Console.WriteLine($"[AuthApi] GoogleLogin completed. PlayerId={player.PlayerId}");
            return await utilService.CastPlayerToInfoAsync(player);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthApi] GoogleLogin failed. Error={ex.Message}");
            return Unauthorized();
        }
    }

    [HttpGet("session")]
    public async Task<ActionResult<PlayerInfo>> GetSession()
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
        {
            Console.WriteLine("[AuthApi] Session unauthorized.");
            return Unauthorized();
        }

        Console.WriteLine($"[AuthApi] Session refresh requested. PlayerId={player.PlayerId}");
        await utilService.SetPlayerOnlineState(player.PlayerId, true);
        var info = await utilService.CastPlayerToInfoAsync(player);
        Console.WriteLine($"[AuthApi] Session refresh completed. PlayerId={player.PlayerId}");
        return info;
    }
}

public record GoogleLoginDto(string IdToken, string? City, string? CountryCode);
