using Ludo.Api.Services;
using Microsoft.AspNetCore.Mvc;
using SignalR.Server.Payments;

namespace Ludo.Api.Controllers;

[ApiController]
[Route("api/nfts")]
public class NftController(
    ApiPlayerContext playerContext,
    CryptoHelper cryptoHelper) : ControllerBase
{
    [HttpPost("mint")]
    public async Task<ActionResult<string>> Mint([FromBody] MintNftDto request)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
        {
            Console.WriteLine($"[NftApi] Mint unauthorized. Amount={request.Amount}");
            return Unauthorized();
        }

        if (request.Amount < 0)
        {
            Console.WriteLine($"[NftApi] Mint rejected. PlayerId={player.PlayerId}, Amount={request.Amount}");
            return "Invalid amount.";
        }

        try
        {
            Console.WriteLine($"[NftApi] Mint requested. PlayerId={player.PlayerId}, Amount={request.Amount}");
            var result = await cryptoHelper.MintNFT(player.PlayerId, request.Amount);
            Console.WriteLine($"[NftApi] Mint completed. PlayerId={player.PlayerId}, Amount={request.Amount}, Result={result}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NftApi] Mint failed. PlayerId={player.PlayerId}, Amount={request.Amount}, Error={ex.Message}");
            return "Failed";
        }
    }
}

public record MintNftDto(int Amount);
