using Ludo.Api.Services;
using Microsoft.AspNetCore.SignalR;
using SignalR.Server;

namespace Ludo.Api.Hubs;

public abstract class LudoHubBase(
    ApiPlayerContext playerContext,
    DatabaseManager databaseManager,
    IHubContext<LudoHub> hubContext) : Hub
{
    protected ApiPlayerContext PlayerContext { get; } = playerContext;
    protected DatabaseManager DatabaseManager { get; } = databaseManager;
    protected IHubContext<LudoHub> HubContext { get; } = hubContext;

    protected string GetAuthToken()
    {
        return Context.GetHttpContext()?.Request.Headers["X-Auth-Token"].FirstOrDefault() ?? string.Empty;
    }
}
