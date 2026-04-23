using Ludo.Api.Services;
using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedCode;
using SignalR.Server.Services;

namespace Ludo.Api.Controllers;

[ApiController]
[Route("api")]
public class SocialController(
    ApiPlayerContext playerContext,
    IDbContextFactory<LudoDbContext> contextFactory,
    FriendsService friendsService) : ControllerBase
{
    [HttpGet("friends")]
    public async Task<ActionResult<List<PlayerCard>>> GetFriends([FromQuery] string type = "All")
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
        {
            Console.WriteLine($"[SocialApi] GetFriends unauthorized. Type={type}");
            return Unauthorized();
        }

        Console.WriteLine($"[SocialApi] GetFriends requested. PlayerId={player.PlayerId}, Type={type}");
        var friends = await friendsService.GetFriends(player, type);
        Console.WriteLine($"[SocialApi] GetFriends completed. PlayerId={player.PlayerId}, Type={type}, Count={friends.Count}");
        return friends;
    }

    [HttpPost("friends/request")]
    public async Task<ActionResult<string>> SendFriendRequest([FromBody] FriendRequestDto request)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
        {
            Console.WriteLine($"[SocialApi] SendFriendRequest unauthorized. ReceiverId={request.ReceiverId}, Status={request.Status}");
            return Unauthorized();
        }

        if (request.ReceiverId <= 0 || string.IsNullOrWhiteSpace(request.Status))
        {
            Console.WriteLine($"[SocialApi] SendFriendRequest rejected. PlayerId={player.PlayerId}, ReceiverId={request.ReceiverId}, Status={request.Status}");
            return BadRequest("Invalid friend request.");
        }

        Console.WriteLine($"[SocialApi] SendFriendRequest requested. PlayerId={player.PlayerId}, ReceiverId={request.ReceiverId}, Status={request.Status}");
        var result = await friendsService.SendFriendRequest(player, request.ReceiverId, request.Status);
        Console.WriteLine($"[SocialApi] SendFriendRequest completed. PlayerId={player.PlayerId}, ReceiverId={request.ReceiverId}, Result={result}");
        return result;
    }

    [HttpGet("players/{playerId:int}/card")]
    public async Task<ActionResult<PlayerCard>> GetPlayerById(int playerId)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
        {
            Console.WriteLine($"[SocialApi] GetPlayerCard unauthorized. TargetPlayerId={playerId}");
            return Unauthorized();
        }

        Console.WriteLine($"[SocialApi] GetPlayerCard requested. PlayerId={player.PlayerId}, TargetPlayerId={playerId}");
        using var ctx = await contextFactory.CreateDbContextAsync();
        var p = await ctx.Players.FirstOrDefaultAsync(x => x.PlayerId == playerId);
        if (p == null)
        {
            Console.WriteLine($"[SocialApi] GetPlayerCard not found. PlayerId={player.PlayerId}, TargetPlayerId={playerId}");
            return NotFound();
        }

        var card = new PlayerCard
        {
            playerID = p.PlayerId,
            name = p.Name,
            pictureUrl = p.PictureUrl,
            rank = ctx.Players.Count(other => other.GamesWon > p.GamesWon) + 1,
            status = "",
            lastGame = false,
            gamesWon = p.GamesWon
        };
        Console.WriteLine($"[SocialApi] GetPlayerCard completed. PlayerId={player.PlayerId}, TargetPlayerId={playerId}");
        return card;
    }

    [HttpGet("leaderboard")]
    public async Task<ActionResult<List<PlayerCard>>> GetLeaderboard()
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
        {
            Console.WriteLine("[SocialApi] GetLeaderboard unauthorized.");
            return Unauthorized();
        }

        Console.WriteLine($"[SocialApi] GetLeaderboard requested. PlayerId={player.PlayerId}");
        using var ctx = await contextFactory.CreateDbContextAsync();
        var topPlayers = await ctx.Players
            .Where(p => p.Role == "Player" && p.GamesWon > 0)
            .OrderByDescending(p => p.GamesWon)
            .Select(p => new PlayerCard
            {
                playerID = p.PlayerId,
                name = p.Name,
                pictureUrl = p.PictureUrl,
                rank = 0,
                status = "",
                lastGame = false,
                gamesWon = p.GamesWon
            })
            .ToListAsync();

        for (var i = 0; i < topPlayers.Count; i++)
            topPlayers[i].rank = i + 1;

        Console.WriteLine($"[SocialApi] GetLeaderboard completed. PlayerId={player.PlayerId}, Count={topPlayers.Count}");
        return topPlayers;
    }

    [HttpGet("leaderboard/tournament/{tournamentType}")]
    public async Task<ActionResult<List<PlayerCard>>> GetTournamentLeaderboard(string tournamentType)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
        {
            Console.WriteLine($"[SocialApi] GetTournamentLeaderboard unauthorized. TournamentType={tournamentType}");
            return Unauthorized();
        }

        Console.WriteLine($"[SocialApi] GetTournamentLeaderboard requested. PlayerId={player.PlayerId}, TournamentType={tournamentType}");
        using var ctx = await contextFactory.CreateDbContextAsync();
        var tournament = await ctx.Tournaments
            .Where(t => t.Name.Contains(tournamentType) && t.TournamentState == State.Active)
            .OrderByDescending(t => t.TournamentId)
            .FirstOrDefaultAsync();

        if (tournament == null)
        {
            Console.WriteLine($"[SocialApi] GetTournamentLeaderboard no active tournament. PlayerId={player.PlayerId}, TournamentType={tournamentType}");
            return new List<PlayerCard>();
        }

        var challengers = await ctx.TournamentChallengers
            .Where(tc => tc.TournamentId == tournament.TournamentId && tc.Score > 0)
            .Include(tc => tc.Player)
            .OrderByDescending(tc => tc.Score)
            .Select(tc => new PlayerCard
            {
                playerID = tc.PlayerId,
                name = tc.Player.Name,
                pictureUrl = tc.Player.PictureUrl,
                rank = 0,
                status = "",
                lastGame = false,
                gamesWon = tc.Score
            })
            .ToListAsync();

        var uniqueChallengers = challengers
            .GroupBy(c => c.playerID)
            .Select(g => g.First())
            .ToList();

        for (var i = 0; i < uniqueChallengers.Count; i++)
            uniqueChallengers[i].rank = i + 1;

        Console.WriteLine($"[SocialApi] GetTournamentLeaderboard completed. PlayerId={player.PlayerId}, TournamentType={tournamentType}, TournamentId={tournament.TournamentId}, Count={uniqueChallengers.Count}");
        return uniqueChallengers;
    }
}

public record FriendRequestDto(int ReceiverId, string Status);
