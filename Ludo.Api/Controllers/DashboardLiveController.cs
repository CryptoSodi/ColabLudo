using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SignalR.Server;
using SignalR.Server.Services;

namespace Ludo.Api.Controllers;

[ApiController]
[Route("api/dashboard/live")]
public class DashboardLiveController(
    IDbContextFactory<LudoDbContext> contextFactory,
    DatabaseManager databaseManager,
    UtilService utilService) : ControllerBase
{
    private async Task<Player?> GetAuthorizedAdminAsync()
    {
        var token = Request.Headers["X-Auth-Token"].FirstOrDefault()
                    ?? Request.Headers.Authorization.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim()
                    ?? string.Empty;
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var playerIdStr = utilService.Decrypt(token);
        if (!int.TryParse(playerIdStr, out var playerId))
            return null;

        await using var ctx = await contextFactory.CreateDbContextAsync();
        var admin = await ctx.Players.AsNoTracking().FirstOrDefaultAsync(p => p.PlayerId == playerId);
        if (admin == null || !admin.IsActive || admin.IsBlocked)
            return null;
        if (admin.Role != "Admin" && admin.Role != "Manager")
            return null;

        return admin;
    }

    [HttpGet("active-matches")]
    public async Task<ActionResult<List<object>>> GetActiveMatches()
    {
        var admin = await GetAuthorizedAdminAsync();
        if (admin == null)
            return Unauthorized();

        var activeRooms = databaseManager._gameRooms.Select(kv =>
        {
            var g = kv.Value.gameDTO;
            string category = "Normal";
            if (g.IsTournamentGame) category = "Tournament";
            else if (g.IsPrivateGame) category = "Private";

            return new
            {
                RoomCode = kv.Key,
                Category = category,
                Type = g.GameType,
                Bet = g.BetAmount,
                PlayerCount = kv.Value.Users.Count,
                Players = kv.Value.Users.Select(u => new
                {
                    u.player.PlayerId,
                    u.player.Name,
                    u.PlayerColor
                }).ToList(),
                Status = kv.Value.engine != null ? "Playing" : "Waiting"
            };
        }).Cast<object>().ToList();

        return activeRooms;
    }

    [HttpGet("past-matches")]
    public async Task<ActionResult<List<object>>> GetPastMatches([FromQuery] int count = 20)
    {
        var admin = await GetAuthorizedAdminAsync();
        if (admin == null)
            return Unauthorized();

        await using var ctx = await contextFactory.CreateDbContextAsync();
        var pastGames = await ctx.Games
            .Include(g => g.MultiPlayer)
            .Where(g => g.State == "Completed")
            .OrderByDescending(g => g.CreatedDate)
            .Take(count)
            .ToListAsync();

        var results = new List<object>();
        foreach (var g in pastGames)
        {
            string category = "Normal";
            if (g.TournamentId.HasValue) category = "Tournament";
            else if (g.IsPrivate) category = "Private";

            var pIds = new List<int?> { g.MultiPlayer.P1, g.MultiPlayer.P2, g.MultiPlayer.P3, g.MultiPlayer.P4 }
                .Where(id => id.HasValue).ToList();

            var players = await ctx.Players
                .Where(p => pIds.Contains(p.PlayerId))
                .ToListAsync();

            string w1 = players.FirstOrDefault(p => p.PlayerId == g.Winner1)?.Name ?? "N/A";
            string w2 = players.FirstOrDefault(p => p.PlayerId == g.Winner2)?.Name ?? "N/A";

            results.Add(new
            {
                Id = g.GameId,
                RoomCode = g.RoomCode,
                Category = category,
                Type = g.GameType,
                Bet = g.BetAmount,
                Winner1 = w1,
                Winner2 = w2,
                Participants = players.Select(p => new { p.PlayerId, p.Name }).ToList(),
                Date = g.CreatedDate
            });
        }

        return results;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetDashboardStats()
    {
        var admin = await GetAuthorizedAdminAsync();
        if (admin == null)
            return Unauthorized();

        await using var ctx = await contextFactory.CreateDbContextAsync();
        var totalPlayers = await ctx.Players.CountAsync(p => p.Role == "Player");
        var activeGames = await ctx.Games.CountAsync(g => g.State == "Active" || g.State == "Playing");
        var completedGames = await ctx.Games.CountAsync(g => g.State == "Completed");
        var activeTournaments = await ctx.Tournaments.CountAsync(t => t.TournamentState == State.Active);
        var pendingCash = await ctx.CashDeposits.CountAsync(d => d.Status == "Pending");
        var totalLUDC = await ctx.PlayerWallet.SumAsync(w => w.AvailableBalance);

        return new
        {
            TotalPlayers = totalPlayers,
            ActiveGames = activeGames,
            CompletedGames = completedGames,
            ActiveTournaments = activeTournaments,
            TotalLUDC = totalLUDC,
            PendingDeposits = pendingCash
        };
    }
}
