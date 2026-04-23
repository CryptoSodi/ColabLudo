using Ludo.Api.Services;
using Microsoft.AspNetCore.Mvc;
using SharedCode;
using SignalR.Server.Services;

namespace Ludo.Api.Controllers;

[ApiController]
[Route("api/tournaments")]
public class TournamentController(
    ApiPlayerContext playerContext,
    TournamentService tournamentService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<TournamentDTO>>> GetAll([FromQuery] string type = "All")
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
        {
            Console.WriteLine($"[TournamentApi] GetAll unauthorized. Type={type}");
            return Unauthorized();
        }

        try
        {
            Console.WriteLine($"[TournamentApi] GetAll requested. PlayerId={player.PlayerId}, Type={type}");
            var tournaments = await tournamentService.GetAllTournaments(player, type);
            Console.WriteLine($"[TournamentApi] GetAll completed. PlayerId={player.PlayerId}, Type={type}, Count={tournaments.Count}");
            return tournaments;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TournamentApi] GetAll failed. PlayerId={player.PlayerId}, Type={type}, Error={ex.Message}");
            return new List<TournamentDTO>();
        }
    }

    [HttpPost("{tournamentId:int}/join")]
    public async Task<ActionResult<TournamentDTO?>> Join(int tournamentId)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
        {
            Console.WriteLine($"[TournamentApi] Join unauthorized. TournamentId={tournamentId}");
            return Unauthorized();
        }

        try
        {
            Console.WriteLine($"[TournamentApi] Join requested. PlayerId={player.PlayerId}, TournamentId={tournamentId}");
            var result = await tournamentService.JoinTournament(player, tournamentId);
            Console.WriteLine($"[TournamentApi] Join completed. PlayerId={player.PlayerId}, TournamentId={tournamentId}, Status={result?.StatusCode ?? "NULL"}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TournamentApi] Join failed. PlayerId={player.PlayerId}, TournamentId={tournamentId}, Error={ex.Message}");
            return null;
        }
    }

    [HttpGet("{tournamentId:int}/results")]
    public async Task<ActionResult<TournamentResultDTO?>> GetResults(int tournamentId)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
        {
            Console.WriteLine($"[TournamentApi] GetResults unauthorized. TournamentId={tournamentId}");
            return Unauthorized();
        }

        Console.WriteLine($"[TournamentApi] GetResults requested. PlayerId={player.PlayerId}, TournamentId={tournamentId}");
        var result = await tournamentService.GetResultsTournament(tournamentId);
        Console.WriteLine($"[TournamentApi] GetResults completed. PlayerId={player.PlayerId}, TournamentId={tournamentId}, HasResult={result != null}");
        return result;
    }
}
