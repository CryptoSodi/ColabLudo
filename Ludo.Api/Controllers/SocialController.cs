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
            return Unauthorized();

        return await friendsService.GetFriends(player, type);
    }

    [HttpPost("friends/request")]
    public async Task<ActionResult<string>> SendFriendRequest([FromBody] FriendRequestDto request)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
            return Unauthorized();

        if (request.ReceiverId <= 0 || string.IsNullOrWhiteSpace(request.Status))
            return BadRequest("Invalid friend request.");

        return await friendsService.SendFriendRequest(player, request.ReceiverId, request.Status);
    }

    [HttpGet("players/{playerId:int}/card")]
    public async Task<ActionResult<PlayerCard>> GetPlayerById(int playerId)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
            return Unauthorized();

        using var ctx = await contextFactory.CreateDbContextAsync();
        var p = await ctx.Players.FirstOrDefaultAsync(x => x.PlayerId == playerId);
        if (p == null)
            return NotFound();

        return new PlayerCard
        {
            playerID = p.PlayerId,
            name = p.Name,
            pictureUrl = p.PictureUrl,
            rank = ctx.Players.Count(other => other.GamesWon > p.GamesWon) + 1,
            status = "",
            lastGame = false,
            gamesWon = p.GamesWon
        };
    }

    [HttpGet("leaderboard")]
    public async Task<ActionResult<List<PlayerCard>>> GetLeaderboard()
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
            return Unauthorized();

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

        return topPlayers;
    }

    [HttpGet("leaderboard/tournament/{tournamentType}")]
    public async Task<ActionResult<List<PlayerCard>>> GetTournamentLeaderboard(string tournamentType)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
            return Unauthorized();

        using var ctx = await contextFactory.CreateDbContextAsync();
        var tournament = await ctx.Tournaments
            .Where(t => t.Name.Contains(tournamentType) && t.TournamentState == State.Active)
            .OrderByDescending(t => t.TournamentId)
            .FirstOrDefaultAsync();

        if (tournament == null)
            return new List<PlayerCard>();

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

        return uniqueChallengers;
    }
}

public record FriendRequestDto(int ReceiverId, string Status);
