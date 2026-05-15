using Ludo.Api.Services;
using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SharedCode.Constants;
using SignalR.Server;
using SignalR.Server.Payments;
using SignalR.Server.Services;

namespace Ludo.Api.Hubs;

public abstract class LudoHubWalletBase(
    ApiPlayerContext playerContext,
    DatabaseManager databaseManager,
    IHubContext<LudoHub> hubContext,
    DailyBonusService dailyBonusService,
    CryptoHelper cryptoHelper,
    UtilService utilService,
    PlayerPresenceTracker presenceTracker,
    IDbContextFactory<LudoDbContext> contextFactory,
    FriendsService friendsService,
    TournamentService tournamentService) : LudoHubTournamentBase(playerContext, databaseManager, hubContext, dailyBonusService, cryptoHelper, utilService, presenceTracker, contextFactory, friendsService, tournamentService)
{
    private readonly IDbContextFactory<LudoDbContext> _contextFactory = contextFactory;

    public async Task<List<WalletBonusHistoryItem>?> GetWalletBonuses()
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null)
        {
            Console.WriteLine("[WalletHub] GetBonuses unauthorized.");
            return null;
        }

        using var ctx = await _contextFactory.CreateDbContextAsync();
        var transactions = await ctx.WalletTransaction
            .AsNoTracking()
            .Where(t => t.PlayerId == player.PlayerId)
            .OrderByDescending(t => t.CreatedDate)
            .Take(200)
            .ToListAsync();

        var items = transactions
            .Where(IsBonusTransaction)
            .Select(t => new WalletBonusHistoryItem
            {
                Id = t.TransactionId,
                Category = ResolveBonusCategory(t),
                Amount = t.Amount,
                Status = t.Status.ToString(),
                Description = t.Description ?? "",
                TransactionReference = t.txId,
                CreatedDate = t.CreatedDate
            })
            .OrderByDescending(t => t.CreatedDate)
            .ToList();

        Console.WriteLine($"[WalletHub] GetBonuses completed. PlayerId={player.PlayerId}, Count={items.Count}");
        return items;
    }

    public async Task<List<WalletDepositHistoryItem>?> GetWalletDeposits()
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null)
        {
            Console.WriteLine("[WalletHub] GetDeposits unauthorized.");
            return null;
        }

        using var ctx = await _contextFactory.CreateDbContextAsync();
        var manualDeposits = await ctx.CashDeposits
            .AsNoTracking()
            .Where(d => d.PlayerId == player.PlayerId)
            .OrderByDescending(d => d.CreatedDate)
            .Select(d => new WalletDepositHistoryItem
            {
                Id = d.Id,
                IsManual = true,
                Source = "Manual Deposit",
                Amount = d.Amount,
                Status = d.Status,
                ReferenceNumber = d.ReferenceNumber,
                PaymentMethod = d.PaymentMethod,
                Description = $"Manual {d.PaymentMethod} deposit",
                ReceiptImageUrl = d.ReceiptImageUrl,
                TransactionReference = d.ReferenceNumber,
                CreatedDate = d.CreatedDate,
                ProcessedDate = d.ProcessedDate,
                AdminNote = d.AdminNote
            })
            .ToListAsync();

        var walletDeposits = await ctx.WalletTransaction
            .AsNoTracking()
            .Where(t => t.PlayerId == player.PlayerId && t.Amount > 0)
            .OrderByDescending(t => t.CreatedDate)
            .Take(200)
            .ToListAsync();

        var ledgerDeposits = walletDeposits
            .Where(IsDepositTransaction)
            .Select(t => new WalletDepositHistoryItem
            {
                Id = t.TransactionId,
                IsManual = false,
                Source = ResolveDepositSource(t),
                Amount = t.Amount,
                Status = t.Status.ToString(),
                ReferenceNumber = t.txId ?? "",
                PaymentMethod = t.IsOnChain ? "On-Chain" : "Internal",
                Description = t.Description ?? "",
                ReceiptImageUrl = "",
                TransactionReference = t.txId ?? "",
                CreatedDate = t.CreatedDate,
                ProcessedDate = t.CreatedDate,
                AdminNote = null
            });

        var items = manualDeposits
            .Concat(ledgerDeposits)
            .OrderByDescending(t => t.CreatedDate)
            .ToList();

        Console.WriteLine($"[WalletHub] GetDeposits completed. PlayerId={player.PlayerId}, Count={items.Count}");
        return items;
    }

    public async Task<List<WalletWithdrawalHistoryItem>?> GetWalletWithdrawals()
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null)
        {
            Console.WriteLine("[WalletHub] GetWithdrawals unauthorized.");
            return null;
        }

        using var ctx = await _contextFactory.CreateDbContextAsync();
        var manualWithdrawals = await ctx.CashWithdrawals
            .AsNoTracking()
            .Where(w => w.PlayerId == player.PlayerId)
            .OrderByDescending(w => w.CreatedDate)
            .Select(w => new WalletWithdrawalHistoryItem
            {
                Id = w.Id,
                IsManual = true,
                Destination = w.DestinationDetails,
                Amount = w.Amount,
                Status = w.Status,
                Method = w.PayoutMethod,
                Description = $"Manual {w.PayoutMethod} withdrawal",
                TransactionReference = "",
                CreatedDate = w.CreatedDate,
                ProcessedDate = w.ProcessedDate,
                AdminNote = w.AdminNote
            })
            .ToListAsync();

        var walletWithdrawals = await ctx.WalletTransaction
            .AsNoTracking()
            .Where(t => t.PlayerId == player.PlayerId && t.Amount < 0)
            .OrderByDescending(t => t.CreatedDate)
            .Take(200)
            .ToListAsync();

        var ledgerWithdrawals = walletWithdrawals
            .Where(IsWithdrawalTransaction)
            .Select(t => new WalletWithdrawalHistoryItem
            {
                Id = t.TransactionId,
                IsManual = false,
                Destination = ResolveWithdrawalDestination(t),
                Amount = Math.Abs(t.Amount),
                Status = t.Status.ToString(),
                Method = t.IsOnChain ? "Wallet" : "Internal",
                Description = t.Description ?? "",
                TransactionReference = t.txId ?? "",
                CreatedDate = t.CreatedDate,
                ProcessedDate = t.CreatedDate,
                AdminNote = null
            });

        var items = manualWithdrawals
            .Concat(ledgerWithdrawals)
            .OrderByDescending(t => t.CreatedDate)
            .ToList();

        Console.WriteLine($"[WalletHub] GetWithdrawals completed. PlayerId={player.PlayerId}, Count={items.Count}");
        return items;
    }

    public async Task<List<WalletGameHistoryItem>?> GetWalletGames()
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null)
        {
            Console.WriteLine("[WalletHub] GetGames unauthorized.");
            return null;
        }

        using var ctx = await _contextFactory.CreateDbContextAsync();
        var transactions = await ctx.WalletTransaction
            .AsNoTracking()
            .Where(t => t.PlayerId == player.PlayerId)
            .OrderByDescending(t => t.CreatedDate)
            .Take(300)
            .ToListAsync();

        var gameTransactions = transactions
            .Where(IsGameTransaction)
            .ToList();

        var roomCodes = gameTransactions
            .Where(t => !string.IsNullOrWhiteSpace(t.RoomCode))
            .Select(t => t.RoomCode!)
            .Distinct()
            .ToList();

        var games = await ctx.Games
            .AsNoTracking()
            .Include(g => g.MultiPlayer)
            .Where(g => roomCodes.Contains(g.RoomCode!))
            .ToListAsync();

        var tournamentIds = games
            .Where(g => g.TournamentId.HasValue)
            .Select(g => g.TournamentId!.Value)
            .Distinct()
            .ToList();

        var tournaments = await ctx.Tournaments
            .AsNoTracking()
            .Where(t => tournamentIds.Contains(t.TournamentId))
            .ToDictionaryAsync(t => t.TournamentId, t => t.Name);

        var participantIds = games
            .SelectMany(g => new[] { g.MultiPlayer?.P1, g.MultiPlayer?.P2, g.MultiPlayer?.P3, g.MultiPlayer?.P4, g.Winner1, g.Winner2 })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var players = await ctx.Players
            .AsNoTracking()
            .Where(p => participantIds.Contains(p.PlayerId))
            .ToDictionaryAsync(p => p.PlayerId, p => p.Name ?? $"Player {p.PlayerId}");

        var gameMap = games
            .Where(g => !string.IsNullOrWhiteSpace(g.RoomCode))
            .GroupBy(g => g.RoomCode!)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedDate).First());

        var items = gameTransactions
            .Select(t =>
            {
                gameMap.TryGetValue(t.RoomCode ?? "", out var game);
                var participants = ResolvePlayerNames(game?.MultiPlayer, players);
                var winners = ResolveWinnerNames(game, players);
                var tournamentName = game?.TournamentId.HasValue == true && tournaments.TryGetValue(game.TournamentId.Value, out var name)
                    ? name
                    : "";

                return new WalletGameHistoryItem
                {
                    Id = t.TransactionId,
                    RoomCode = t.RoomCode ?? "",
                    Mode = game?.TournamentId.HasValue == true ? "Tournament" : "Direct",
                    Result = ResolveGameResult(t),
                    BetAmount = game?.BetAmount ?? 0m,
                    NetAmount = t.Amount,
                    Description = t.Description ?? "",
                    Status = t.Status.ToString(),
                    TournamentId = game?.TournamentId,
                    TournamentName = tournamentName,
                    Players = participants,
                    Winners = winners,
                    CreatedDate = t.CreatedDate
                };
            })
            .OrderByDescending(t => t.CreatedDate)
            .ToList();

        Console.WriteLine($"[WalletHub] GetGames completed. PlayerId={player.PlayerId}, Count={items.Count}");
        return items;
    }

    private async Task<Player?> TryGetAuthenticatedPlayerAsync()
    {
        var token = GetAuthToken();
        if (string.IsNullOrWhiteSpace(token)) return null;
        var authContext = new DefaultHttpContext();
        authContext.Request.Headers["X-Auth-Token"] = token;
        return await PlayerContext.GetAuthenticatedPlayerAsync(authContext.Request);
    }

    private static bool IsBonusTransaction(LudoServer.Models.WalletTransaction transaction)
    {
        var description = (transaction.Description ?? string.Empty).ToLowerInvariant();
        return transaction.Type == LudoServer.Models.TransactionType.DailyBonus
               || description.Contains("bonus")
               || description.Contains("refer")
               || description.Contains("airdrop")
               || description.Contains("signup");
    }

    private static string ResolveBonusCategory(LudoServer.Models.WalletTransaction transaction)
    {
        var description = (transaction.Description ?? string.Empty).ToLowerInvariant();
        if (transaction.Type == LudoServer.Models.TransactionType.DailyBonus || description.Contains("daily"))
            return "Daily Bonus";
        if (description.Contains("refer"))
            return "Refer Bonus";
        if (description.Contains("airdrop"))
            return "Airdrop";
        if (description.Contains("signup"))
            return "Signup Bonus";
        return "Bonus";
    }

    private static bool IsDepositTransaction(LudoServer.Models.WalletTransaction transaction)
    {
        var description = transaction.Description ?? string.Empty;
        if (transaction.Type != LudoServer.Models.TransactionType.Deposit)
            return false;

        return !description.Contains("Game Refund", StringComparison.OrdinalIgnoreCase)
               && !description.Contains("Internal transfer from", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveDepositSource(LudoServer.Models.WalletTransaction transaction)
    {
        var description = transaction.Description ?? string.Empty;
        if (description.Contains("Approved", StringComparison.OrdinalIgnoreCase))
            return "Manual Deposit";
        if (transaction.IsOnChain)
            return "Direct Deposit";
        return "Wallet Deposit";
    }

    private static bool IsWithdrawalTransaction(LudoServer.Models.WalletTransaction transaction)
    {
        var description = transaction.Description ?? string.Empty;
        return transaction.Type == LudoServer.Models.TransactionType.Withdrawal
               && !description.Contains("Internal transfer to", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveWithdrawalDestination(LudoServer.Models.WalletTransaction transaction)
    {
        if (transaction.IsOnChain)
            return "External Wallet";

        var description = transaction.Description ?? string.Empty;
        if (description.Contains("manual", StringComparison.OrdinalIgnoreCase))
            return "Manual Payout";

        return "Wallet Transfer";
    }

    private static bool IsGameTransaction(LudoServer.Models.WalletTransaction transaction)
    {
        var description = transaction.Description ?? string.Empty;
        return transaction.Type == LudoServer.Models.TransactionType.GameWin
               || description.Contains("Game Fee", StringComparison.OrdinalIgnoreCase)
               || description.Contains("Tournament Fee", StringComparison.OrdinalIgnoreCase)
               || description.Contains("Game Refund", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveGameResult(LudoServer.Models.WalletTransaction transaction)
    {
        if (transaction.Type == LudoServer.Models.TransactionType.GameWin)
            return "Won";

        var description = transaction.Description ?? string.Empty;
        if (description.Contains("Refund", StringComparison.OrdinalIgnoreCase))
            return "Refunded";
        if (description.Contains("Tournament Fee", StringComparison.OrdinalIgnoreCase))
            return "Tournament Joined";
        return "Played";
    }

    private static List<string> ResolvePlayerNames(MultiPlayer? multiPlayer, IReadOnlyDictionary<int, string> players)
    {
        if (multiPlayer == null)
            return new List<string>();

        var ids = new[] { multiPlayer.P1, multiPlayer.P2, multiPlayer.P3, multiPlayer.P4 }
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct();

        return ids
            .Select(id => players.TryGetValue(id, out var name) ? name : $"Player {id}")
            .ToList();
    }

    private static List<string> ResolveWinnerNames(Game? game, IReadOnlyDictionary<int, string> players)
    {
        if (game == null)
            return new List<string>();

        var ids = new[] { game.Winner1, game.Winner2 }
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct();

        return ids
            .Select(id => players.TryGetValue(id, out var name) ? name : $"Player {id}")
            .ToList();
    }
}
