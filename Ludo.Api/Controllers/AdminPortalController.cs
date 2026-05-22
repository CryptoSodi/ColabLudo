using Ludo.Api.Services;
using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SignalR.Server.Payments;
using SignalR.Server.Services;

namespace Ludo.Api.Controllers;

[ApiController]
[Route("api/admin-portal")]
public class AdminPortalController(
    ApiPlayerContext playerContext,
    IDbContextFactory<LudoDbContext> contextFactory,
    CryptoHelper cryptoHelper,
    IConfiguration configuration) : ControllerBase
{
    private async Task<Player?> GetAuthorizedAdminAsync(bool allowManager = true)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null || !player.IsActive || player.IsBlocked) return null;
        if (player.Role == "Admin") return player;
        if (allowManager && player.Role == "Manager") return player;
        return null;
    }

    [HttpGet("rpc-config")]
    public async Task<ActionResult<object>> GetClientRpcConfig()
    {
        var admin = await GetAuthorizedAdminAsync();
        if (admin == null) return Unauthorized();

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

    [HttpGet("players")]
    public async Task<ActionResult<List<object>>> GetAllPlayers()
    {
        var admin = await GetAuthorizedAdminAsync();
        if (admin == null) return Unauthorized();

        await using var ctx = await contextFactory.CreateDbContextAsync();
        var players = await ctx.Players
            .OrderByDescending(p => p.CreatedDate)
            .Select(p => new
            {
                Id = p.PlayerId,
                Name = p.Name,
                Email = p.Email,
                PhoneNumber = p.PhoneNumber,
                City = p.City,
                Role = p.Role,
                Wins = p.GamesWon,
                Played = p.GamesPlayed,
                Rank = ctx.Players.Count(other => other.GamesWon > p.GamesWon) + 1,
                CreatedDate = p.CreatedDate
            })
            .ToListAsync<object>();
        return players;
    }

    [HttpGet("players/top")]
    public async Task<ActionResult<List<object>>> GetTopPlayers()
    {
        var admin = await GetAuthorizedAdminAsync();
        if (admin == null) return Unauthorized();

        await using var ctx = await contextFactory.CreateDbContextAsync();
        var topPlayers = await ctx.Players
            .Where(p => p.Role == "Player" && p.GamesWon > 0)
            .OrderByDescending(p => p.GamesWon)
            .Take(10)
            .Select(p => new
            {
                Id = p.PlayerId,
                Name = p.Name,
                Wins = p.GamesWon,
                Played = p.GamesPlayed
            })
            .ToListAsync<object>();
        return topPlayers;
    }

    [HttpGet("players/{playerId:int}/dashboard")]
    public async Task<ActionResult<object>> GetPlayerDashboard(int playerId)
    {
        var admin = await GetAuthorizedAdminAsync();
        if (admin == null) return Unauthorized();

        await using var ctx = await contextFactory.CreateDbContextAsync();
        var player = await ctx.Players.Include(p => p.Wallets).FirstOrDefaultAsync(p => p.PlayerId == playerId);
        if (player == null) return NotFound();

        var rank = await ctx.Players.CountAsync(other => other.Role == "Player" && other.GamesWon > player.GamesWon) + 1;
        var transactions = await ctx.WalletTransaction.Where(t => t.PlayerId == playerId)
            .OrderByDescending(t => t.CreatedDate).Take(20)
            .Select(t => new { t.Amount, t.Type, Status = t.Status.ToString(), t.Description, t.RoomCode, Date = t.CreatedDate })
            .ToListAsync();

        var manualDeposits = await ctx.CashDeposits.Where(d => d.PlayerId == playerId).OrderByDescending(d => d.CreatedDate).Take(10).ToListAsync();
        var manualWithdrawals = await ctx.CashWithdrawals.Where(w => w.PlayerId == playerId).OrderByDescending(w => w.CreatedDate).Take(10).ToListAsync();
        var games = await ctx.Games.Include(g => g.MultiPlayer)
            .Where(g => g.State == "Completed" && (g.MultiPlayer.P1 == playerId || g.MultiPlayer.P2 == playerId || g.MultiPlayer.P3 == playerId || g.MultiPlayer.P4 == playerId))
            .OrderByDescending(g => g.CreatedDate).Take(10).ToListAsync();

        var matchHistory = new List<object>();
        foreach (var g in games)
        {
            var pIds = new List<int?> { g.MultiPlayer.P1, g.MultiPlayer.P2, g.MultiPlayer.P3, g.MultiPlayer.P4 }
                .Where(id => id.HasValue && id != playerId).ToList();
            var opponents = await ctx.Players.Where(p => pIds.Contains(p.PlayerId)).Select(p => p.Name).ToListAsync();
            matchHistory.Add(new { Id = g.GameId, Type = g.GameType, Bet = g.BetAmount, IsWin = g.Winner1 == playerId || g.Winner2 == playerId, Opponents = string.Join(", ", opponents), Date = g.CreatedDate });
        }

        var wallet = await cryptoHelper.EnsurePlayerWalletExists(playerId, CurrencyType.LUDC);
        return new
        {
            Id = player.PlayerId,
            Name = player.Name,
            Picture = player.PictureUrl,
            Email = player.Email,
            PhoneNumber = player.PhoneNumber,
            Played = player.GamesPlayed,
            Wins = player.GamesWon,
            Lost = player.GamesLost,
            BestWin = player.BestWin,
            TotalWin = player.TotalWin,
            TotalLost = player.TotalLost,
            Rank = rank,
            LUDC = wallet?.AvailableBalance ?? 0,
            WalletAddress = wallet?.WalletAddress ?? "",
            City = player.City,
            IsBlocked = player.IsBlocked,
            Role = player.Role,
            Transactions = transactions,
            ManualDeposits = manualDeposits,
            ManualWithdrawals = manualWithdrawals,
            RecentGames = matchHistory
        };
    }

    [HttpPost("players/{playerId:int}/block")]
    public async Task<ActionResult<string>> BlockPlayer(int playerId, [FromBody] BlockRequest request)
    {
        var admin = await GetAuthorizedAdminAsync(allowManager: false);
        if (admin == null) return Unauthorized("Unauthorized.");

        await using var ctx = await contextFactory.CreateDbContextAsync();
        var player = await ctx.Players.FirstOrDefaultAsync(p => p.PlayerId == playerId);
        if (player == null) return NotFound("Not Found");
        if (player.Name == "SYSTEM" || player.Role == "SYSTEM") return BadRequest("Protected Account.");

        player.IsBlocked = request.IsBlocked;
        player.IsActive = !request.IsBlocked;
        ctx.Players.Update(player);
        await ctx.SaveChangesAsync();
        return "Success";
    }

    [HttpPost("players/{playerId:int}/role")]
    public async Task<ActionResult<string>> UpdatePlayerRole(int playerId, [FromBody] RoleRequest request)
    {
        var admin = await GetAuthorizedAdminAsync(allowManager: false);
        if (admin == null) return Unauthorized("Unauthorized.");

        await using var ctx = await contextFactory.CreateDbContextAsync();
        var player = await ctx.Players.FirstOrDefaultAsync(p => p.PlayerId == playerId);
        if (player == null) return NotFound("Not Found");
        if (player.Name == "SYSTEM" || player.Role == "SYSTEM") return BadRequest("Protected Account.");

        player.Role = request.NewRole;
        ctx.Players.Update(player);
        await ctx.SaveChangesAsync();
        return "Success";
    }

    [HttpPost("players/{playerId:int}/adjust-balance")]
    public async Task<ActionResult<string>> AdjustPlayerBalance(int playerId, [FromBody] AdjustBalanceRequest request)
    {
        var admin = await GetAuthorizedAdminAsync(allowManager: false);
        if (admin == null) return Unauthorized("Unauthorized: Admin only.");

        await using var ctx = await contextFactory.CreateDbContextAsync();
        var player = await ctx.Players.FirstOrDefaultAsync(p => p.PlayerId == playerId);
        if (player != null && (player.Name == "SYSTEM" || player.Role == "SYSTEM")) return BadRequest("Error: System account is protected.");

        var wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == playerId);
        if (wallet == null) return BadRequest("Wallet Not Found");

        wallet.AvailableBalance += request.Amount;
        ctx.WalletTransaction.Add(new WalletTransaction
        {
            PlayerId = playerId,
            Amount = request.Amount,
            Type = request.Amount > 0 ? TransactionType.Deposit : TransactionType.Withdrawal,
            Status = WalletTransactionStatus.Completed,
            Description = $"Admin Adjustment ({admin.Name}): {request.Reason}",
            OperationId = Guid.NewGuid(),
            BalanceAfter = wallet.AvailableBalance
        });

        ctx.PlayerWallet.Update(wallet);
        await ctx.SaveChangesAsync();
        return "Success";
    }

    [HttpGet("game-audit/{gameId:int}")]
    public async Task<ActionResult<object>> GetGameAudit(int gameId)
    {
        var admin = await GetAuthorizedAdminAsync();
        if (admin == null) return Unauthorized();

        await using var ctx = await contextFactory.CreateDbContextAsync();
        var g = await ctx.Games.Include(x => x.MultiPlayer).FirstOrDefaultAsync(x => x.GameId == gameId);
        if (g == null) return NotFound();
        return await BuildAuditObject(ctx, g);
    }

    [HttpGet("game-audit-room/{roomCode}")]
    public async Task<ActionResult<object>> GetGameAuditByRoom(string roomCode)
    {
        var admin = await GetAuthorizedAdminAsync();
        if (admin == null) return Unauthorized();

        await using var ctx = await contextFactory.CreateDbContextAsync();
        var g = await ctx.Games.Include(x => x.MultiPlayer).FirstOrDefaultAsync(x => x.RoomCode == roomCode);
        if (g == null) return NotFound();
        return await BuildAuditObject(ctx, g);
    }

    [HttpGet("tournaments")]
    public async Task<ActionResult<List<object>>> GetAllTournamentsAdmin()
    {
        var admin = await GetAuthorizedAdminAsync();
        if (admin == null) return Unauthorized();

        await using var ctx = await contextFactory.CreateDbContextAsync();
        var tournaments = await ctx.Tournaments.OrderByDescending(t => t.CreatedDate).Select(t => new
        {
            Id = t.TournamentId,
            Name = t.Name,
            EntryFee = t.EntryFee,
            State = t.TournamentState.ToString(),
            StartDate = t.StartDate,
            EndDate = t.EndDate,
            Prize1 = t.Prize1,
            Participants = t.TournamentChallengers.Count,
            GamesPlayed = ctx.Games.Count(g => g.TournamentId == t.TournamentId),
            Winner1 = t.Winner1
        }).ToListAsync<object>();
        return tournaments;
    }

    [HttpGet("tournaments/{id:int}/audit")]
    public async Task<ActionResult<object>> GetTournamentAudit(int id)
    {
        var admin = await GetAuthorizedAdminAsync();
        if (admin == null) return Unauthorized();

        await using var ctx = await contextFactory.CreateDbContextAsync();
        var t = await ctx.Tournaments.Include(x => x.TournamentChallengers).FirstOrDefaultAsync(x => x.TournamentId == id);
        if (t == null) return NotFound();

        var totalParticipants = t.TournamentChallengers.Count;
        var totalRevenue = totalParticipants * t.EntryFee;
        var totalPrizes = t.Prize1 + t.Prize2 + t.Prize3;
        var netResult = totalRevenue - totalPrizes;

        var leaderBoard = await ctx.TournamentChallengers.Include(tc => tc.Player).Where(tc => tc.TournamentId == id)
            .OrderByDescending(tc => tc.Score)
            .Select(tc => new { tc.PlayerId, tc.Player.Name, tc.Score, tc.CreatedDate }).ToListAsync();

        var games = await ctx.Games.Where(g => g.TournamentId == id).OrderByDescending(g => g.CreatedDate)
            .Select(g => new { Id = g.GameId, g.RoomCode, g.State, g.BetAmount, g.CreatedDate }).ToListAsync();

        return new
        {
            t.TournamentId,
            t.Name,
            t.StartDate,
            t.EndDate,
            t.TournamentState,
            t.EntryFee,
            Finance = new { TotalParticipants = totalParticipants, TotalRevenue = totalRevenue, TotalPrizes = totalPrizes, NetResult = netResult },
            Participants = leaderBoard.Select((tc, index) => new { tc.PlayerId, tc.Name, tc.Score, Rank = index + 1, Joined = tc.CreatedDate }).ToList(),
            Games = games
        };
    }

    [HttpPost("tournaments")]
    public async Task<ActionResult<string>> CreateTournament([FromBody] CreateTournamentRequest request)
    {
        var admin = await GetAuthorizedAdminAsync();
        if (admin == null) return Unauthorized("Unauthorized.");

        await using var ctx = await contextFactory.CreateDbContextAsync();
        var t = new Tournament
        {
            Name = request.Name,
            City = request.City,
            EntryFee = request.EntryFee,
            Prize1 = request.P1,
            Prize2 = request.P2,
            Prize3 = request.P3,
            StartDate = request.Start.Date,
            EndDate = request.End.Date,
            IsRepeatable = request.IsRepeatable,
            TournamentState = State.Active,
            CreatedDate = DateTime.UtcNow
        };
        ctx.Tournaments.Add(t);
        await ctx.SaveChangesAsync();
        return "Success";
    }

    [HttpPost("tournaments/{id:int}/close")]
    public async Task<ActionResult<string>> CloseTournamentManually(int id)
    {
        var admin = await GetAuthorizedAdminAsync();
        if (admin == null) return Unauthorized("Unauthorized.");

        await using var ctx = await contextFactory.CreateDbContextAsync();
        var t = await ctx.Tournaments.FirstOrDefaultAsync(x => x.TournamentId == id);
        if (t == null) return NotFound("Not Found");
        t.TournamentState = State.Completed;
        t.EndDate = DateTime.UtcNow;
        ctx.Tournaments.Update(t);
        await ctx.SaveChangesAsync();
        return "Success";
    }

    [HttpGet("finance/pending-deposits")]
    public async Task<ActionResult<List<object>>> GetPendingDeposits()
    {
        var admin = await GetAuthorizedAdminAsync();
        if (admin == null) return Unauthorized();

        await using var ctx = await contextFactory.CreateDbContextAsync();
        var deposits = await ctx.CashDeposits.Include(d => d.Player).Where(d => d.Status == "Pending").OrderByDescending(d => d.CreatedDate)
            .Select(d => new { Id = d.Id, PlayerName = d.Player.Name, playerId = d.PlayerId, ReferenceNumber = d.ReferenceNumber, Amount = d.Amount, Method = d.PaymentMethod, ReceiptUrl = d.ReceiptImageUrl, Date = d.CreatedDate })
            .ToListAsync<object>();
        return deposits;
    }

    [HttpGet("finance/pending-withdrawals")]
    public async Task<ActionResult<List<object>>> GetPendingWithdrawals()
    {
        var admin = await GetAuthorizedAdminAsync();
        if (admin == null) return Unauthorized();

        await using var ctx = await contextFactory.CreateDbContextAsync();
        var withdrawals = await ctx.CashWithdrawals.Include(w => w.Player).Where(w => w.Status == "Pending").OrderByDescending(w => w.CreatedDate)
            .Select(w => new { Id = w.Id, PlayerName = w.Player.Name, playerId = w.PlayerId, Amount = w.Amount, Method = w.PayoutMethod, DestinationDetails = w.DestinationDetails, Date = w.CreatedDate })
            .ToListAsync<object>();
        return withdrawals;
    }

    [HttpPost("finance/deposits/{depositId:int}/process")]
    public async Task<ActionResult<string>> ProcessDeposit(int depositId, [FromBody] ProcessRequest request)
    {
        var admin = await GetAuthorizedAdminAsync();
        if (admin == null) return Unauthorized("Unauthorized.");
        if (request.Action != "Approved" && request.Action != "Rejected") return BadRequest("Invalid action.");

        await using var ctx = await contextFactory.CreateDbContextAsync();
        var deposit = await ctx.CashDeposits.FirstOrDefaultAsync(d => d.Id == depositId);
        if (deposit == null || deposit.Status != "Pending") return NotFound("Not Found");

        if (request.Action == "Approved")
        {
            var wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == deposit.PlayerId);
            if (wallet == null) return BadRequest("Wallet Not Found");
            wallet.AvailableBalance += deposit.Amount;
            ctx.WalletTransaction.Add(new WalletTransaction
            {
                PlayerId = deposit.PlayerId,
                Amount = deposit.Amount,
                Type = TransactionType.Deposit,
                Status = WalletTransactionStatus.Completed,
                Description = $"Approved {deposit.PaymentMethod} deposit ({deposit.ReferenceNumber}). {request.Note}",
                OperationId = Guid.NewGuid(),
                BalanceAfter = wallet.AvailableBalance
            });
            ctx.PlayerWallet.Update(wallet);
        }

        deposit.Status = request.Action;
        deposit.AdminNote = request.Note;
        deposit.ProcessedByAdminId = admin.PlayerId;
        deposit.ProcessedDate = DateTime.UtcNow;
        ctx.CashDeposits.Update(deposit);
        await ctx.SaveChangesAsync();
        return "Success";
    }

    [HttpPost("finance/withdrawals/{withdrawalId:int}/process")]
    public async Task<ActionResult<string>> ProcessWithdrawal(int withdrawalId, [FromBody] ProcessRequest request)
    {
        var admin = await GetAuthorizedAdminAsync();
        if (admin == null) return Unauthorized("Unauthorized.");
        if (request.Action != "Approved" && request.Action != "Rejected") return BadRequest("Invalid action.");

        await using var ctx = await contextFactory.CreateDbContextAsync();
        var withdrawal = await ctx.CashWithdrawals.FirstOrDefaultAsync(w => w.Id == withdrawalId);
        if (withdrawal == null || withdrawal.Status != "Pending") return NotFound("Not Found");

        if (request.Action == "Approved")
        {
            var wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == withdrawal.PlayerId && w.AddressType == "LUDC");
            if (wallet == null) return BadRequest("Wallet Not Found");
            if (wallet.AvailableBalance < withdrawal.Amount) return BadRequest("Insufficient balance.");
            wallet.AvailableBalance -= withdrawal.Amount;
            ctx.WalletTransaction.Add(new WalletTransaction
            {
                PlayerId = withdrawal.PlayerId,
                Amount = withdrawal.Amount,
                Type = TransactionType.Withdrawal,
                Status = WalletTransactionStatus.Completed,
                Description = $"Approved manual {withdrawal.PayoutMethod} payout. {request.Note}",
                OperationId = Guid.NewGuid(),
                BalanceAfter = wallet.AvailableBalance
            });
            ctx.PlayerWallet.Update(wallet);
        }

        withdrawal.Status = request.Action;
        withdrawal.AdminNote = request.Note;
        withdrawal.ProcessedByAdminId = admin.PlayerId;
        withdrawal.ProcessedDate = DateTime.UtcNow;
        ctx.CashWithdrawals.Update(withdrawal);
        await ctx.SaveChangesAsync();
        return "Success";
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

    public class BlockRequest { public bool IsBlocked { get; set; } }
    public class RoleRequest { public string NewRole { get; set; } = "Player"; }
    public class AdjustBalanceRequest { public decimal Amount { get; set; } public string Reason { get; set; } = "Manual Adjustment"; }
    public class ProcessRequest { public string Action { get; set; } = ""; public string Note { get; set; } = ""; }
    public class CreateTournamentRequest
    {
        public string Name { get; set; } = "";
        public string City { get; set; } = "";
        public decimal EntryFee { get; set; }
        public decimal P1 { get; set; }
        public decimal P2 { get; set; }
        public decimal P3 { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public bool IsRepeatable { get; set; }
    }
}
