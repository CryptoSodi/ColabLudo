using Ludo.Api.Services;
using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SignalR.Server.Services;

namespace Ludo.Api.Controllers;

[ApiController]
[Route("api/user-portal")]
public class UserPortalController(
    ApiPlayerContext playerContext,
    IDbContextFactory<LudoDbContext> contextFactory,
    IConfiguration configuration,
    UtilService utilService) : ControllerBase
{
    [HttpGet("rpc-config")]
    public async Task<ActionResult<object>> GetClientRpcConfig()
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null || !player.IsActive || player.IsBlocked || player.Role != "Player")
            return Unauthorized();

        var clientRpcUrl = configuration["Solana:ClientRpcUrl"] ?? string.Empty;
        var provider = clientRpcUrl.Contains("fluxrpc", StringComparison.OrdinalIgnoreCase) ? "FluxRPC" : "Custom";

        return new
        {
            HasRpc = !string.IsNullOrWhiteSpace(clientRpcUrl),
            RpcUrl = string.Empty,
            DisplayLabel = provider == "FluxRPC" ? "FluxRPC Mainnet" : "Configured Solana RPC",
            Provider = provider
        };
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<object>> GetPlayerDashboard()
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null || !player.IsActive || player.IsBlocked || player.Role != "Player")
            return Unauthorized();

        await using var ctx = await contextFactory.CreateDbContextAsync();
        var p = await ctx.Players.FirstOrDefaultAsync(x => x.PlayerId == player.PlayerId);
        if (p == null) return NotFound();

        var wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == p.PlayerId && w.AddressType == "LUDC");
        var tx = await ctx.WalletTransaction.Where(t => t.PlayerId == p.PlayerId).OrderByDescending(t => t.CreatedDate).ToListAsync();
        var manualDeposits = await ctx.CashDeposits.Where(d => d.PlayerId == p.PlayerId).OrderByDescending(d => d.CreatedDate).Take(20).ToListAsync();
        var manualWithdrawals = await ctx.CashWithdrawals.Where(w => w.PlayerId == p.PlayerId).OrderByDescending(w => w.CreatedDate).Take(20).ToListAsync();
        var recentGames = await ctx.Games.Include(g => g.MultiPlayer)
            .Where(g => g.MultiPlayer.P1 == p.PlayerId || g.MultiPlayer.P2 == p.PlayerId || g.MultiPlayer.P3 == p.PlayerId || g.MultiPlayer.P4 == p.PlayerId)
            .OrderByDescending(g => g.CreatedDate).Take(20).ToListAsync();

        return new
        {
            Id = p.PlayerId,
            Name = p.Name,
            Email = p.Email,
            PhoneNumber = p.PhoneNumber,
            City = p.City,
            Picture = p.PictureUrl,
            Rank = ctx.Players.Count(other => other.GamesWon > p.GamesWon) + 1,
            Played = p.GamesPlayed,
            Wins = p.GamesWon,
            Lost = Math.Max(0, p.GamesPlayed - p.GamesWon),
            Ludc = wallet?.AvailableBalance ?? 0m,
            WalletAddress = wallet?.WalletAddress ?? "",
            BestWin = tx.Where(t => t.Amount > 0).Select(t => (decimal?)t.Amount).Max() ?? 0m,
            TotalWin = tx.Where(t => t.Amount > 0).Sum(t => t.Amount),
            TotalLost = Math.Abs(tx.Where(t => t.Amount < 0).Sum(t => t.Amount)),
            RecentGames = recentGames.Select(g => new
            {
                Id = g.GameId,
                Type = g.GameType,
                Bet = g.BetAmount,
                IsWin = g.Winner1 == p.PlayerId || g.Winner2 == p.PlayerId,
                Opponents = "Players",
                Date = g.CreatedDate
            }).ToList(),
            ManualDeposits = manualDeposits,
            ManualWithdrawals = manualWithdrawals,
            TournamentHistory = new List<object>()
        };
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<List<object>>> GetTransactionsFiltered([FromQuery] string type = "All", [FromQuery] DateTime? start = null)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null || !player.IsActive || player.IsBlocked || player.Role != "Player")
            return Unauthorized();

        await using var ctx = await contextFactory.CreateDbContextAsync();
        var query = ctx.WalletTransaction.Where(t => t.PlayerId == player.PlayerId).AsQueryable();

        if (!string.Equals(type, "All", StringComparison.OrdinalIgnoreCase))
        {
            if (Enum.TryParse<TransactionType>(type, true, out var parsedType))
                query = query.Where(t => t.Type == parsedType);
        }
        if (start.HasValue) query = query.Where(t => t.CreatedDate >= start.Value);

        var results = await query.OrderByDescending(t => t.CreatedDate).Select(t => new
        {
            t.Amount,
            t.Type,
            Status = t.Status.ToString(),
            t.Description,
            t.RoomCode,
            Date = t.CreatedDate
        }).ToListAsync();

        return results.Cast<object>().ToList();
    }

    [HttpGet("game-audit/{gameId:int}")]
    public async Task<ActionResult<object>> GetGameAudit(int gameId)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null || !player.IsActive || player.IsBlocked || player.Role != "Player")
            return Unauthorized();

        await using var ctx = await contextFactory.CreateDbContextAsync();
        var g = await ctx.Games.Include(x => x.MultiPlayer).FirstOrDefaultAsync(x => x.GameId == gameId);
        if (g == null) return NotFound();
        return await BuildAuditObject(ctx, g);
    }

    [HttpGet("game-audit-room/{roomCode}")]
    public async Task<ActionResult<object>> GetGameAuditByRoom(string roomCode)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null || !player.IsActive || player.IsBlocked || player.Role != "Player")
            return Unauthorized();

        await using var ctx = await contextFactory.CreateDbContextAsync();
        var g = await ctx.Games.Include(x => x.MultiPlayer).FirstOrDefaultAsync(x => x.RoomCode == roomCode);
        if (g == null) return NotFound();
        return await BuildAuditObject(ctx, g);
    }

    private static async Task<object> BuildAuditObject(LudoDbContext ctx, Game g)
    {
        string category = "Normal";
        if (g.TournamentId.HasValue) category = "Tournament";
        else if (g.IsPrivate) category = "Private";

        var pIds = new List<int?> { g.MultiPlayer.P1, g.MultiPlayer.P2, g.MultiPlayer.P3, g.MultiPlayer.P4 }
            .Where(id => id.HasValue).ToList();
        var players = await ctx.Players.Where(p => pIds.Contains(p.PlayerId)).ToListAsync();

        return new
        {
            Id = g.GameId,
            RoomCode = g.RoomCode,
            Category = category,
            Type = g.GameType,
            Bet = g.BetAmount,
            Winner1 = players.FirstOrDefault(p => p.PlayerId == g.Winner1)?.Name ?? "N/A",
            Winner2 = players.FirstOrDefault(p => p.PlayerId == g.Winner2)?.Name ?? "N/A",
            Participants = players.Select(p => new { p.PlayerId, p.Name }).ToList(),
            Date = g.CreatedDate
        };
    }
}
