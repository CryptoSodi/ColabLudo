using Ludo.Api.Services;
using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SharedCode.Constants;
using SignalR.Server;
using SignalR.Server.Payments;
using SignalR.Server.Services;

namespace Ludo.Api.Hubs;

public abstract class LudoHubProfileBase(
    ApiPlayerContext playerContext,
    DatabaseManager databaseManager,
    IHubContext<LudoHub> hubContext,
    DailyBonusService dailyBonusService,
    CryptoHelper cryptoHelper,
    UtilService utilService,
    PlayerPresenceTracker presenceTracker,
    IDbContextFactory<LudoDbContext> contextFactory) : LudoHubNftBase(playerContext, databaseManager, hubContext, contextFactory, dailyBonusService, cryptoHelper)
{
    private readonly UtilService _utilService = utilService;
    private readonly PlayerPresenceTracker _presenceTracker = presenceTracker;
    private readonly IDbContextFactory<LudoDbContext> _contextFactory = contextFactory;

    public async Task<PlayerInfo?> GetProfile()
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null)
        {
            Console.WriteLine("[ProfileHub] GetProfile unauthorized.");
            return null;
        }

        Console.WriteLine($"[ProfileHub] GetProfile requested. PlayerId={player.PlayerId}");
        var info = await _utilService.CastPlayerToInfoAsync(player);
        Console.WriteLine($"[ProfileHub] GetProfile completed. PlayerId={player.PlayerId}");
        return info;
    }

    public async Task<SharedCode.Constants.PlayerWallet?> GetWallet()
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null)
        {
            Console.WriteLine("[ProfileHub] GetWallet unauthorized.");
            return null;
        }

        Console.WriteLine($"[ProfileHub] GetWallet requested. PlayerId={player.PlayerId}");
        var info = await _utilService.CastPlayerToInfoAsync(player);
        if (info.Wallet == null)
        {
            Console.WriteLine($"[ProfileHub] GetWallet not found. PlayerId={player.PlayerId}");
            return null;
        }

        Console.WriteLine($"[ProfileHub] GetWallet completed. PlayerId={player.PlayerId}, Balance={info.Wallet.AvailableBalance}");
        return info.Wallet;
    }

    public async Task<SessionSyncInfo?> SyncSession()
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null)
        {
            Console.WriteLine("[ProfileHub] SyncSession unauthorized.");
            return null;
        }

        Console.WriteLine($"[ProfileHub] SyncSession requested. PlayerId={player.PlayerId}");
        if (_presenceTracker.TryMarkOnlineTransition(player.PlayerId))
            await _utilService.SetPlayerOnlineState(player.PlayerId, true, touchLastLogin: true);

        _presenceTracker.RecordPing(player.PlayerId);

        using var ctx = await _contextFactory.CreateDbContextAsync();
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

        Console.WriteLine($"[ProfileHub] SyncSession completed. PlayerId={player.PlayerId}, Balance={wallet?.AvailableBalance ?? 0m}");
        return new SessionSyncInfo
        {
            PlayerId = player.PlayerId,
            IsOnline = true,
            ServerTime = DateTime.UtcNow,
            Wallet = wallet
        };
    }

    private async Task<Player?> TryGetAuthenticatedPlayerAsync()
    {
        var token = GetAuthToken();
        if (string.IsNullOrWhiteSpace(token)) return null;
        var authContext = new DefaultHttpContext();
        authContext.Request.Headers["X-Auth-Token"] = token;
        return await PlayerContext.GetAuthenticatedPlayerAsync(authContext.Request);
    }
}
