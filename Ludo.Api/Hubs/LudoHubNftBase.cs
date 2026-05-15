using Ludo.Api.Services;
using LudoServer.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SignalR.Server;
using SignalR.Server.Payments;
using SignalR.Server.Services;

namespace Ludo.Api.Hubs;

public abstract class LudoHubNftBase(
    ApiPlayerContext playerContext,
    DatabaseManager databaseManager,
    IHubContext<LudoHub> hubContext,
    IDbContextFactory<LudoDbContext> contextFactory,
    DailyBonusService dailyBonusService,
    CryptoHelper cryptoHelper) : LudoHubDailyBonusBase(playerContext, databaseManager, hubContext, contextFactory, dailyBonusService)
{
    private readonly CryptoHelper _cryptoHelper = cryptoHelper;

    public async Task<string> MintNFT(int amount)
    {
        var token = GetAuthToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            Console.WriteLine($"[NftHub] Mint unauthorized. Amount={amount}, Reason=MissingAuthToken");
            return "Unauthorized";
        }

        var authContext = new DefaultHttpContext();
        authContext.Request.Headers["X-Auth-Token"] = token;
        var player = await PlayerContext.GetAuthenticatedPlayerAsync(authContext.Request);
        if (player == null)
        {
            Console.WriteLine($"[NftHub] Mint unauthorized. Amount={amount}, Reason=InvalidAuthToken");
            return "Unauthorized";
        }

        if (amount < 0)
        {
            Console.WriteLine($"[NftHub] Mint rejected. PlayerId={player.PlayerId}, Amount={amount}");
            return "Invalid amount.";
        }

        try
        {
            Console.WriteLine($"[NftHub] Mint requested. PlayerId={player.PlayerId}, Amount={amount}");
            var result = await _cryptoHelper.MintNFT(player.PlayerId, amount);
            Console.WriteLine($"[NftHub] Mint completed. PlayerId={player.PlayerId}, Amount={amount}, Result={result}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NftHub] Mint failed. PlayerId={player.PlayerId}, Amount={amount}, Error={ex.Message}");
            return "Failed";
        }
    }
}
