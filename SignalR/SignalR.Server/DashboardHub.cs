using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SharedCode;
using SignalR.Server.Services;
using SignalR.Server.Payments;

namespace SignalR.Server
{
    public class DashboardHub : Hub
    {
        private readonly IDbContextFactory<LudoDbContext> _contextFactory;
        private readonly GoogleAuthService _googleAuthService;
        private readonly UtilService _utilService;
        private readonly DatabaseManager _databaseManager;
        private readonly CryptoHelper _crypto;

        public DashboardHub(IDbContextFactory<LudoDbContext> contextFactory, 
                            GoogleAuthService googleAuthService, 
                            UtilService utilService,
                            DatabaseManager databaseManager,
                            CryptoHelper crypto)
        {
            _contextFactory = contextFactory;
            _googleAuthService = googleAuthService;
            _utilService = utilService;
            _databaseManager = databaseManager;
            _crypto = crypto;
        }

        public async Task<bool> ValidateSession(string authToken, string requiredRole)
        {
            try
            {
                if (string.IsNullOrEmpty(authToken)) return false;

                // 1. Decrypt and find player
                var playerIdStr = _utilService.Decrypt(authToken);
                if (!int.TryParse(playerIdStr, out int playerId)) return false;

                using var ctx = _contextFactory.CreateDbContext();
                var player = await ctx.Players.AsNoTracking().FirstOrDefaultAsync(p => p.PlayerId == playerId);

                if (player == null || player.IsBlocked || !player.IsActive) return false;

                // 2. Role Check
                if (requiredRole == "Admin")
                {
                    // Admin area requires Admin or Manager
                    if (player.Role != "Admin" && player.Role != "Manager") return false;
                }
                else if (requiredRole == "Player")
                {
                    // User area requires Player role
                    if (player.Role != "Player") return false;
                }

                return true;
            }
            catch { return false; }
        }

        public async Task<List<object>> GetActiveMatches()
        {
            // Fetch live data directly from the server's memory (DatabaseManager)
            var activeRooms = _databaseManager._gameRooms.Select(kv => {
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
                    Players = kv.Value.Users.Select(u => new {
                        u.player.PlayerId,
                        u.player.Name,
                        u.PlayerColor
                    }).ToList(),
                    Status = kv.Value.engine != null ? "Playing" : "Waiting"
                };
            }).ToList<object>();

            return activeRooms;
        }

        public async Task<List<object>> GetPastMatches(int count = 20)
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                var pastGames = await ctx.Games
                    .Include(g => g.MultiPlayer)
                    .Where(g => g.State == "Completed")
                    .OrderByDescending(g => g.CreatedDate)
                    .Take(count)
                    .ToListAsync();

                var results = new List<object>();
                foreach (var g in pastGames)
                {
                    // 1. Resolve Category
                    string category = "Normal";
                    if (g.TournamentId.HasValue) category = "Tournament";
                    else if (g.IsPrivate) category = "Private";

                    // 2. Fetch participant names
                    var pIds = new List<int?> { g.MultiPlayer.P1, g.MultiPlayer.P2, g.MultiPlayer.P3, g.MultiPlayer.P4 }
                        .Where(id => id.HasValue).ToList();
                    
                    var players = await ctx.Players
                        .Where(p => pIds.Contains(p.PlayerId))
                        .ToListAsync();

                    // 3. Resolve Winner Names
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching past matches: {ex.Message}");
                return new List<object>();
            }
        }

        public async Task<object> GoogleAuthentication(string idToken, string city, string countryCode)
        {
            try
            {
                var player = await _googleAuthService.GoogleAuthentication(idToken, city, countryCode);
                if (player == null) return null;

                return new
                {
                    AuthToken = player.AuthToken,
                    PlayerId = player.PlayerId,
                    Role = player.Role
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Auth Error in DashboardHub: {ex.Message}");
                return null;
            }
        }

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"Dashboard User connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine($"Dashboard User disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task<object> GetDashboardStats()
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                
                var totalPlayers = await ctx.Players.CountAsync(p => p.Role == "Player");
                var activeGames = await ctx.Games.CountAsync(g => g.State == "Active" || g.State == "Playing");
                var completedGames = await ctx.Games.CountAsync(g => g.State == "Completed");
                var activeTournaments = await ctx.Tournaments.CountAsync(t => t.TournamentState == State.Active);

                // Correct pending count from the new CashDeposits table
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting dashboard stats: {ex.Message}");
                return null;
            }
        }

        public async Task<List<object>> GetAllPlayers()
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting all players: {ex.Message}");
                return new List<object>();
            }
        }

        public async Task<object> GetPlayerDashboard(int playerId)
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                var player = await ctx.Players
                    .Include(p => p.Wallets)
                    .FirstOrDefaultAsync(p => p.PlayerId == playerId);

                if (player == null) return null;

                // REAL-TIME RANKING
                var rank = await ctx.Players
                    .CountAsync(other => other.Role == "Player" && other.GamesWon > player.GamesWon) + 1;

                // RECENT TRANSACTIONS
                var transactions = await ctx.WalletTransaction
                    .Where(t => t.PlayerId == playerId)
                    .OrderByDescending(t => t.CreatedDate)
                    .Take(20)
                    .Select(t => new {
                        t.Amount,
                        t.Type,
                        Status = t.Status.ToString(),
                        t.Description,
                        t.RoomCode, 
                        Date = t.CreatedDate
                    })
                    .ToListAsync();

                // MANUAL DEPOSITS
                var manualDeposits = await ctx.CashDeposits
                    .Where(d => d.PlayerId == playerId)
                    .OrderByDescending(d => d.CreatedDate)
                    .Take(10)
                    .ToListAsync();

                // MATCH HISTORY (With Opponent Names)
                var games = await ctx.Games
                    .Include(g => g.MultiPlayer)
                    .Where(g => g.State == "Completed" && 
                               (g.MultiPlayer.P1 == playerId || g.MultiPlayer.P2 == playerId || 
                                g.MultiPlayer.P3 == playerId || g.MultiPlayer.P4 == playerId))
                    .OrderByDescending(g => g.CreatedDate)
                    .Take(10)
                    .ToListAsync();

                var matchHistory = new List<object>();
                foreach(var g in games)
                {
                    var pIds = new List<int?> { g.MultiPlayer.P1, g.MultiPlayer.P2, g.MultiPlayer.P3, g.MultiPlayer.P4 }
                        .Where(id => id.HasValue && id != playerId)
                        .ToList();
                    
                    var opponents = await ctx.Players
                        .Where(p => pIds.Contains(p.PlayerId))
                        .Select(p => p.Name)
                        .ToListAsync();

                    matchHistory.Add(new {
                        Id = g.GameId,
                        Type = g.GameType,
                        Bet = g.BetAmount,
                        IsWin = g.Winner1 == playerId || g.Winner2 == playerId,
                        Opponents = string.Join(", ", opponents),
                        Date = g.CreatedDate
                    });
                }

                // 🛑 NEW: Ensure Wallet exists so WalletAddress is populated
                var wallet = await _crypto.EnsurePlayerWalletExists(playerId, SignalR.Server.Payments.CurrencyType.LUDC);

                return new
                {
                    Id = player.PlayerId,
                    Name = player.Name,
                    Picture = player.PictureUrl,
                    Email = player.Email,
                    PhoneNumber = player.PhoneNumber, // Added
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
                    RecentGames = matchHistory
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting player dashboard: {ex.Message}");
                return null;
            }
        }

        public async Task<List<object>> GetTransactionsFiltered(int playerId, string type, DateTime? startDate)
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                var query = ctx.WalletTransaction
                    .Where(t => t.PlayerId == playerId);

                if (type != "All")
                {
                    if (int.TryParse(type, out int typeInt))
                        query = query.Where(t => t.Type == (TransactionType)typeInt);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(t => t.CreatedDate >= startDate.Value);
                }

                var results = await query
                    .OrderByDescending(t => t.CreatedDate)
                    .Select(t => new {
                        t.Amount,
                        t.Type,
                        Status = t.Status.ToString(),
                        t.Description,
                        t.RoomCode,
                        Date = t.CreatedDate
                    })
                    .ToListAsync();

                return results.Cast<object>().ToList();
            }
            catch (Exception ex) { return new List<object>(); }
        }

        public async Task<string> InitiateWithdrawal(string authToken, string destinationAddress, decimal amount)
        {
            try
            {
                if (amount <= 0) return "Invalid amount.";
                
                var playerIdStr = _utilService.Decrypt(authToken);
                if (!int.TryParse(playerIdStr, out int playerId)) return "Unauthorized.";

                using var ctx = _contextFactory.CreateDbContext();
                var player = await ctx.Players.FirstOrDefaultAsync(p => p.PlayerId == playerId);
                if (player == null || player.IsBlocked) return "Account blocked or not found.";

                // Use injected CryptoHelper
                var result = _crypto.Withdraw(player, destinationAddress, amount);
                
                return result;
            }
            catch (Exception ex) { return "Error: " + ex.Message; }
        }

        public async Task<object> GetGameAudit(int gameId)
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                var g = await ctx.Games
                    .Include(g => g.MultiPlayer)
                    .FirstOrDefaultAsync(x => x.GameId == gameId);
                
                if (g == null) return null;

                string category = "Normal";
                if (g.TournamentId.HasValue) category = "Tournament";
                else if (g.IsPrivate) category = "Private";

                var pIds = new List<int?> { g.MultiPlayer.P1, g.MultiPlayer.P2, g.MultiPlayer.P3, g.MultiPlayer.P4 }
                    .Where(id => id.HasValue).ToList();
                
                var players = await ctx.Players
                    .Where(p => pIds.Contains(p.PlayerId))
                    .ToListAsync();

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
            catch (Exception ex) { return null; }
        }

        public async Task<object> GetGameAuditByRoom(string roomCode)
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                var g = await ctx.Games
                    .Include(g => g.MultiPlayer)
                    .FirstOrDefaultAsync(x => x.RoomCode == roomCode);
                
                if (g == null) return null;

                string category = "Normal";
                if (g.TournamentId.HasValue) category = "Tournament";
                else if (g.IsPrivate) category = "Private";

                var pIds = new List<int?> { g.MultiPlayer.P1, g.MultiPlayer.P2, g.MultiPlayer.P3, g.MultiPlayer.P4 }
                    .Where(id => id.HasValue).ToList();
                
                var players = await ctx.Players
                    .Where(p => pIds.Contains(p.PlayerId))
                    .ToListAsync();

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
            catch (Exception ex) { return null; }
        }

        public async Task<string> AdjustPlayerBalance(int adminId, int playerId, decimal amount, string reason)
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                // 1. RBAC: Only Admin can adjust balance
                var admin = await ctx.Players.FirstOrDefaultAsync(p => p.PlayerId == adminId);
                if (admin == null || admin.Role != "Admin") return "Unauthorized: Admin only.";

                // 2. Protection: SYSTEM account cannot be modified
                var player = await ctx.Players.FirstOrDefaultAsync(p => p.PlayerId == playerId);
                if (player != null && (player.Name == "SYSTEM" || player.Role == "SYSTEM")) return "Error: System account is protected.";

                var wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == playerId);
                if (wallet == null) return "Wallet Not Found";

                wallet.AvailableBalance += amount;
                
                var transaction = new WalletTransaction
                {
                    PlayerId = playerId,
                    Amount = amount,
                    Type = amount > 0 ? TransactionType.Deposit : TransactionType.Withdrawal,
                    Status = WalletTransactionStatus.Completed,
                    Description = $"Admin Adjustment ({admin.Name}): {reason}",
                    OperationId = Guid.NewGuid(),
                    BalanceAfter = wallet.AvailableBalance
                };

                ctx.WalletTransaction.Add(transaction);
                ctx.PlayerWallet.Update(wallet);
                await ctx.SaveChangesAsync();
                
                await Clients.User(playerId.ToString()).SendAsync("UpdateBalance", wallet.AvailableBalance);
                return "Success";
            }
            catch (Exception ex) { return "Error: " + ex.Message; }
        }

        public async Task<string> UpdatePlayerRole(int adminId, int playerId, string newRole)
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                var admin = await ctx.Players.FirstOrDefaultAsync(p => p.PlayerId == adminId);
                if (admin == null || admin.Role != "Admin") return "Unauthorized.";

                var player = await ctx.Players.FirstOrDefaultAsync(p => p.PlayerId == playerId);
                if (player == null) return "Not Found";
                if (player.Name == "SYSTEM" || player.Role == "SYSTEM") return "Protected Account.";

                player.Role = newRole;
                ctx.Players.Update(player);
                await ctx.SaveChangesAsync();
                return "Success";
            }
            catch (Exception ex) { return "Error: " + ex.Message; }
        }

        public async Task<string> BlockPlayer(int adminId, int playerId, bool isBlocked)
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                var admin = await ctx.Players.FirstOrDefaultAsync(p => p.PlayerId == adminId);
                if (admin == null || admin.Role != "Admin") return "Unauthorized.";

                var player = await ctx.Players.FirstOrDefaultAsync(p => p.PlayerId == playerId);
                if (player == null) return "Not Found";
                if (player.Name == "SYSTEM" || player.Role == "SYSTEM") return "Protected Account.";

                // 1. Update Database States
                player.IsBlocked = isBlocked;
                player.IsActive = !isBlocked; // If blocked, account is not active
                
                ctx.Players.Update(player);
                await ctx.SaveChangesAsync();

                // 2. Notify the LudoHub (Mobile App) to kick the user if online
                var ludoHubContext = (IHubContext<LudoHub>)Context.GetHttpContext().RequestServices.GetService(typeof(IHubContext<LudoHub>));
                if (ludoHubContext != null && isBlocked)
                {
                    // Find all active connections for this player and send block signal
                    await ludoHubContext.Clients.User(playerId.ToString()).SendAsync("AccountStatusUpdate", "ACCOUNT_BLOCKED");
                }

                return "Success";
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        public async Task<string> SubmitManualDeposit(int playerId, decimal amount, string method, string receiptUrl)
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                var deposit = new CashDeposit
                {
                    PlayerId = playerId,
                    Amount = amount,
                    PaymentMethod = method,
                    ReceiptImageUrl = receiptUrl,
                    Status = "Pending",
                    CreatedDate = DateTime.UtcNow
                };

                ctx.CashDeposits.Add(deposit);
                await ctx.SaveChangesAsync();
                return "Success";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error submitting deposit: {ex.Message}");
                return "Error";
            }
        }

        public async Task<List<object>> GetPendingDeposits()
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                var deposits = await ctx.CashDeposits
                    .Include(d => d.Player)
                    .Where(d => d.Status == "Pending")
                    .OrderByDescending(d => d.CreatedDate)
                    .Select(d => new
                    {
                        Id = d.Id,
                        PlayerName = d.Player.Name,
                        playerId = d.PlayerId,
                        Amount = d.Amount,
                        Method = d.PaymentMethod,
                        ReceiptUrl = d.ReceiptImageUrl,
                        Date = d.CreatedDate
                    })
                    .ToListAsync<object>();
                return deposits;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting pending deposits: {ex.Message}");
                return new List<object>();
            }
        }

        public async Task<string> ApproveDeposit(int depositId, string note)
        {
            try
            {
                // Verify caller is admin (In production, use [Authorize(Roles="Admin")] and check Claims)
                using var ctx = _contextFactory.CreateDbContext();
                var deposit = await ctx.CashDeposits.FirstOrDefaultAsync(d => d.Id == depositId);
                if (deposit == null || deposit.Status != "Pending") return "Not Found";

                var wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == deposit.PlayerId);
                if (wallet == null) return "Wallet Not Found";

                // 1. Credit Wallet
                wallet.AvailableBalance += deposit.Amount;
                
                // 2. Log Transaction
                var transaction = new WalletTransaction
                {
                    PlayerId = deposit.PlayerId,
                    Amount = deposit.Amount,
                    Type = TransactionType.Deposit,
                    Status = WalletTransactionStatus.Completed,
                    Description = $"Approved {deposit.PaymentMethod} deposit. {note}",
                    OperationId = Guid.NewGuid(),
                    BalanceAfter = wallet.AvailableBalance
                };

                deposit.Status = "Approved";
                deposit.AdminNote = note;
                deposit.ProcessedDate = DateTime.UtcNow;

                ctx.WalletTransaction.Add(transaction);
                ctx.CashDeposits.Update(deposit);
                ctx.PlayerWallet.Update(wallet);

                await ctx.SaveChangesAsync();
                return "Success";
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        public async Task<string> RejectDeposit(int depositId, string note)
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                var deposit = await ctx.CashDeposits.FirstOrDefaultAsync(d => d.Id == depositId);
                if (deposit == null || deposit.Status != "Pending") return "Not Found";

                deposit.Status = "Rejected";
                deposit.AdminNote = note;
                deposit.ProcessedDate = DateTime.UtcNow;

                ctx.CashDeposits.Update(deposit);
                await ctx.SaveChangesAsync();
                return "Success";
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        public async Task<object> GetPlayerByEmail(string email)
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                var player = await ctx.Players.FirstOrDefaultAsync(p => p.Email == email);
                if (player == null) return null;

                // Reuse the same detailed data aggregation as the normal dashboard
                return await GetPlayerDashboard(player.PlayerId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching player by email: {ex.Message}");
                return null;
            }
        }

        public async Task<List<object>> GetTopPlayers()
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting top players: {ex.Message}");
                return new List<object>();
            }
        }
        
        public async Task<List<object>> GetActiveTournaments()
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                var tournaments = await ctx.Tournaments
                    .Where(t => t.TournamentState == State.Active)
                    .Select(t => new
                    {
                        Id = t.TournamentId,
                        Name = t.Name,
                        EntryFee = t.EntryFee,
                        EndDate = t.EndDate,
                        ParticipantsCount = t.TournamentChallengers.Count
                    })
                    .ToListAsync<object>();

                return tournaments;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting active tournaments: {ex.Message}");
                return new List<object>();
            }
        }

        public async Task<List<object>> GetAllTournamentsAdmin()
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                var tournaments = await ctx.Tournaments
                    .OrderByDescending(t => t.CreatedDate)
                    .Select(t => new
                    {
                        Id = t.TournamentId,
                        Name = t.Name,
                        EntryFee = t.EntryFee,
                        State = t.TournamentState.ToString(),
                        StartDate = t.StartDate,
                        EndDate = t.EndDate,
                        Prize1 = t.Prize1,
                        Participants = t.TournamentChallengers.Count,
                        // Count games linked to this tournament
                        GamesPlayed = ctx.Games.Count(g => g.TournamentId == t.TournamentId),
                        Winner1 = t.Winner1
                    })
                    .ToListAsync<object>();
                return tournaments;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting admin tournaments: {ex.Message}");
                return new List<object>();
            }
        }

        public async Task<object> GetTournamentAudit(int id)
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                var t = await ctx.Tournaments
                    .Include(x => x.TournamentChallengers)
                    .FirstOrDefaultAsync(x => x.TournamentId == id);
                if (t == null) return null;

                // 1. FINANCIAL ANALYTICS
                var totalParticipants = t.TournamentChallengers.Count;
                var totalRevenue = totalParticipants * t.EntryFee;
                var totalPrizes = t.Prize1 + t.Prize2 + t.Prize3;
                var netResult = totalRevenue - totalPrizes;

                // 2. LEADERBOARD
                var leaderBoard = await ctx.TournamentChallengers
                    .Include(tc => tc.Player)
                    .Where(tc => tc.TournamentId == id)
                    .OrderByDescending(tc => tc.Score)
                    .Select(tc => new {
                        tc.PlayerId,
                        tc.Player.Name,
                        tc.Score, // Wins
                        tc.CreatedDate
                    })
                    .ToListAsync();

                // 3. MATCH LIST
                var games = await ctx.Games
                    .Where(g => g.TournamentId == id)
                    .OrderByDescending(g => g.CreatedDate)
                    .Select(g => new {
                        Id = g.GameId, // Added this field
                        g.RoomCode,
                        g.State,
                        g.BetAmount,
                        g.CreatedDate
                    })
                    .ToListAsync();

                return new {
                    t.TournamentId,
                    t.Name,
                    t.StartDate,
                    t.EndDate,
                    t.TournamentState,
                    t.EntryFee,
                    Finance = new {
                        TotalParticipants = totalParticipants,
                        TotalRevenue = totalRevenue,
                        TotalPrizes = totalPrizes,
                        NetResult = netResult
                    },
                    Participants = leaderBoard.Select((tc, index) => new {
                        tc.PlayerId,
                        tc.Name,
                        tc.Score,
                        Rank = index + 1,
                        Joined = tc.CreatedDate
                    }).ToList(),
                    Games = games
                };
            }
            catch (Exception ex) { 
                Console.WriteLine($"Error auditing tournament: {ex.Message}");
                return null; 
            }
        }

        public async Task<string> CreateTournament(string name, string city, decimal entryFee, decimal p1, decimal p2, decimal p3, DateTime start, DateTime end, bool isRepeatable)
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                var t = new Tournament
                {
                    Name = name,
                    City = city,
                    EntryFee = entryFee,
                    Prize1 = p1,
                    Prize2 = p2,
                    Prize3 = p3,
                    StartDate = start.Date, // Set to 00:00:00
                    EndDate = end.Date, // Set to 00:00:00
                    IsRepeatable = isRepeatable,
                    TournamentState = State.Active,
                    CreatedDate = DateTime.UtcNow
                };
                ctx.Tournaments.Add(t);
                await ctx.SaveChangesAsync();
                return "Success";
            }
            catch (Exception ex) { return "Error: " + ex.Message; }
        }

        public async Task<string> CloseTournamentManually(int id)
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                var t = await ctx.Tournaments.FirstOrDefaultAsync(x => x.TournamentId == id);
                if (t == null) return "Not Found";
                
                t.TournamentState = State.Completed;
                t.EndDate = DateTime.UtcNow;
                ctx.Tournaments.Update(t);
                await ctx.SaveChangesAsync();
                return "Success";
            }
            catch (Exception ex) { return "Error: " + ex.Message; }
        }
    }
}