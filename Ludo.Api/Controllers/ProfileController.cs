using Ludo.Api.Services;
using LudoServer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedCode.Constants;
using SignalR.Server.Services;

namespace Ludo.Api.Controllers;

[ApiController]
[Route("api")]
public class ProfileController(
    ApiPlayerContext playerContext,
    UtilService utilService,
    PlayerPresenceTracker presenceTracker,
    IDbContextFactory<LudoDbContext> contextFactory) : ControllerBase
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

    [HttpGet("session/sync")]
    public async Task<ActionResult<SessionSyncInfo>> SyncSession()
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
        {
            Console.WriteLine("[ProfileApi] SyncSession unauthorized.");
            return Unauthorized();
        }

        Console.WriteLine($"[ProfileApi] SyncSession requested. PlayerId={player.PlayerId}");
        if (presenceTracker.TryMarkOnlineTransition(player.PlayerId))
            await utilService.SetPlayerOnlineState(player.PlayerId, true, touchLastLogin: true);

        presenceTracker.RecordPing(player.PlayerId);

        using var ctx = await contextFactory.CreateDbContextAsync();
        var wallet = await ctx.PlayerWallet
            .AsNoTracking()
            .Where(w => w.PlayerId == player.PlayerId && w.AddressType == "LUDC")
            .Select(w => new PlayerWalletSyncInfo
            {
                WalletId = w.WalletId,
                PlayerId = w.PlayerId,
                AddressType = w.AddressType,
                WalletAddress = w.WalletAddress,
                AvailableBalance = w.AvailableBalance
            })
            .FirstOrDefaultAsync();

        Console.WriteLine($"[ProfileApi] SyncSession completed. PlayerId={player.PlayerId}, Balance={wallet?.AvailableBalance ?? 0m}");
        return new SessionSyncInfo
        {
            PlayerId = player.PlayerId,
            IsOnline = true,
            ServerTime = DateTime.UtcNow,
            Wallet = wallet
        };
    }
}
