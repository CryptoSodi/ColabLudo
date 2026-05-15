using Ludo.Api.Services;
using LudoServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using SignalR.Server;
using SignalR.Server.Payments;
using SignalR.Server.Services;

namespace Ludo.Api.Hubs;

public sealed class LudoHub : LudoHubPaymentBase
{
    public LudoHub(
        ApiPlayerContext playerContext,
        DatabaseManager databaseManager,
        IHubContext<LudoHub> hubContext,
        DailyBonusService dailyBonusService,
        CryptoHelper cryptoHelper,
        UtilService utilService,
        PlayerPresenceTracker presenceTracker,
        IDbContextFactory<LudoDbContext> contextFactory,
        FriendsService friendsService,
        TournamentService tournamentService,
        LudcPaymentProvider ludcPaymentProvider,
        JupiterSwapService jupiterSwapService)
        : base(playerContext, databaseManager, hubContext, dailyBonusService, cryptoHelper, utilService, presenceTracker, contextFactory, friendsService, tournamentService, ludcPaymentProvider, jupiterSwapService)
    {
    }
}
