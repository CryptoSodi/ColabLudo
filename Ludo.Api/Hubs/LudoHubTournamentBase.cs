using Ludo.Api.Services;
using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SharedCode;
using SignalR.Server;
using SignalR.Server.Payments;
using SignalR.Server.Services;

namespace Ludo.Api.Hubs;

public abstract class LudoHubTournamentBase(
    ApiPlayerContext playerContext,
    DatabaseManager databaseManager,
    IHubContext<LudoHub> hubContext,
    DailyBonusService dailyBonusService,
    CryptoHelper cryptoHelper,
    UtilService utilService,
    PlayerPresenceTracker presenceTracker,
    IDbContextFactory<LudoDbContext> contextFactory,
    FriendsService friendsService,
    TournamentService tournamentService) : LudoHubSocialBase(playerContext, databaseManager, hubContext, dailyBonusService, cryptoHelper, utilService, presenceTracker, contextFactory, friendsService)
{
    private readonly TournamentService _tournamentService = tournamentService;

    public async Task<List<TournamentDTO>> GetTournaments(string type = "All")
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null)
        {
            Console.WriteLine($"[TournamentHub] GetAll unauthorized. Type={type}");
            return new List<TournamentDTO>();
        }

        try
        {
            Console.WriteLine($"[TournamentHub] GetAll requested. PlayerId={player.PlayerId}, Type={type}");
            var tournaments = await _tournamentService.GetAllTournaments(player, type);
            Console.WriteLine($"[TournamentHub] GetAll completed. PlayerId={player.PlayerId}, Type={type}, Count={tournaments.Count}");
            return tournaments;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TournamentHub] GetAll failed. PlayerId={player.PlayerId}, Type={type}, Error={ex.Message}");
            return new List<TournamentDTO>();
        }
    }

    public async Task<TournamentDTO?> JoinTournament(int tournamentId)
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null)
        {
            Console.WriteLine($"[TournamentHub] Join unauthorized. TournamentId={tournamentId}");
            return null;
        }

        try
        {
            Console.WriteLine($"[TournamentHub] Join requested. PlayerId={player.PlayerId}, TournamentId={tournamentId}");
            var result = await _tournamentService.JoinTournament(player, tournamentId);
            Console.WriteLine($"[TournamentHub] Join completed. PlayerId={player.PlayerId}, TournamentId={tournamentId}, Status={result?.StatusCode ?? "NULL"}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TournamentHub] Join failed. PlayerId={player.PlayerId}, TournamentId={tournamentId}, Error={ex.Message}");
            return null;
        }
    }

    public async Task<TournamentResultDTO?> GetTournamentResults(int tournamentId)
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null)
        {
            Console.WriteLine($"[TournamentHub] GetResults unauthorized. TournamentId={tournamentId}");
            return null;
        }

        Console.WriteLine($"[TournamentHub] GetResults requested. PlayerId={player.PlayerId}, TournamentId={tournamentId}");
        var result = await _tournamentService.GetResultsTournament(tournamentId);
        Console.WriteLine($"[TournamentHub] GetResults completed. PlayerId={player.PlayerId}, TournamentId={tournamentId}, HasResult={result != null}");
        return result;
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
