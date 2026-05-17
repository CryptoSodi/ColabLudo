using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SignalR.Server.Payments;
using SignalR.Server.Services;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Types;
using Solnet.Wallet;
using System.Text.Json;
using SignalR.Server;
namespace Ludo.Api.Hubs;

public class DashboardHub : Hub
{
    private static string GetJsonString(JsonElement parent, string propertyName, string fallback = "")
    {
        if (!parent.TryGetProperty(propertyName, out var value))
            return fallback;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? fallback,
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => fallback
        };
    }

    private static (string Mint, int Decimals, string? TokenProgram)? GetSupportedAsset(string assetCode, string ludcMintAddress)
    {
        return (assetCode ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "SOL" => (SolMint, SolDecimals, null),
            "USDC" => (UsdcMint, UsdcDecimals, StandardTokenProgram),
            "LUDC" => (ludcMintAddress, LudcDecimals, Token2022Program),
            _ => null
        };
    }

    private static string DeriveAssociatedTokenAddress(string ownerAddress, string mintAddress, string tokenProgramAddress)
    {
        var owner = new PublicKey(ownerAddress);
        var mint = new PublicKey(mintAddress);
        var tokenProgram = new PublicKey(tokenProgramAddress);
        PublicKey.TryFindProgramAddress(
            new[] { owner.KeyBytes, tokenProgram.KeyBytes, mint.KeyBytes },
            AssociatedTokenAccountProgram.ProgramIdKey,
            out var ata,
            out _);
        return ata.Key;
    }

    private async Task<decimal> GetTokenBalanceAsync(string ownerAddress, string mintAddress, string tokenProgramAddress)
    {
        var ata = DeriveAssociatedTokenAddress(ownerAddress, mintAddress, tokenProgramAddress);
        var balance = await _solanaRpc.GetTokenAccountBalanceAsync(ata, Commitment.Confirmed);
        if (!balance.WasSuccessful || balance.Result?.Value == null)
            return 0m;

        return decimal.TryParse(balance.Result.Value.UiAmountString, out var parsed) ? parsed : 0m;
    }

    private readonly IDbContextFactory<LudoDbContext> _contextFactory;
    private readonly UtilService _utilService;
    private readonly DatabaseManager _databaseManager;
    private readonly CryptoHelper _crypto;
    private readonly LudcPaymentProvider _ludcPaymentProvider;
    private readonly JupiterSwapService _jupiterSwapService;
    private readonly string _clientRpcUrl;
    private readonly IRpcClient _solanaRpc = ClientFactory.GetClient(Cluster.MainNet);
    private const string SolMint = "So11111111111111111111111111111111111111112";
    private const string UsdcMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v";
    private const string StandardTokenProgram = "TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA";
    private const string Token2022Program = "TokenzQdBNbLqP5VEhdkAS6EPFLC1PHnBqCXEpPxuEb";
    private const int SolDecimals = 9;
    private const int UsdcDecimals = 6;
    private const int MaxSwapSlippageBps = 150;
    private const int LudcDecimals = 9;

    public DashboardHub(IDbContextFactory<LudoDbContext> contextFactory,
                        UtilService utilService,
                        DatabaseManager databaseManager,
                        CryptoHelper crypto,
                        LudcPaymentProvider ludcPaymentProvider,
                        JupiterSwapService jupiterSwapService,
                        IConfiguration configuration)
    {
        _contextFactory = contextFactory;
        _utilService = utilService;
        _databaseManager = databaseManager;
        _crypto = crypto;
        _ludcPaymentProvider = ludcPaymentProvider;
        _jupiterSwapService = jupiterSwapService;
        _clientRpcUrl = configuration["Solana:ClientRpcUrl"] ?? string.Empty;
    }

    private async Task<Player?> GetAuthorizedAdmin(string authToken, bool allowManager = true)
    {
        if (string.IsNullOrWhiteSpace(authToken))
            return null;

        var playerIdStr = _utilService.Decrypt(authToken);
        if (!int.TryParse(playerIdStr, out int playerId))
            return null;

        using var ctx = _contextFactory.CreateDbContext();
        var admin = await ctx.Players.FirstOrDefaultAsync(p => p.PlayerId == playerId);
        if (admin == null || !admin.IsActive || admin.IsBlocked)
            return null;

        if (admin.Role == "Admin")
            return admin;

        if (allowManager && admin.Role == "Manager")
            return admin;

        return null;
    }

    private async Task<Player?> GetAuthorizedPlayer(string authToken)
    {
        if (string.IsNullOrWhiteSpace(authToken))
            return null;

        var playerIdStr = _utilService.Decrypt(authToken);
        if (!int.TryParse(playerIdStr, out int playerId))
            return null;

        using var ctx = _contextFactory.CreateDbContext();
        var player = await ctx.Players.FirstOrDefaultAsync(p => p.PlayerId == playerId);
        if (player == null || !player.IsActive || player.IsBlocked || player.Role != "Player")
            return null;

        return player;
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

    public async Task<object> GetClientRpcConfig(string authToken, string requiredRole)
    {
        var isValid = await ValidateSession(authToken, requiredRole);
        if (!isValid)
        {
            return new
            {
                HasRpc = false,
                RpcUrl = string.Empty,
                Provider = "Unauthorized"
            };
        }

        var provider = _clientRpcUrl.Contains("fluxrpc", StringComparison.OrdinalIgnoreCase)
            ? "FluxRPC"
            : "Custom";

        return new
        {
            HasRpc = !string.IsNullOrWhiteSpace(_clientRpcUrl),
            RpcUrl = string.Empty,
            DisplayLabel = provider == "FluxRPC" ? "FluxRPC Mainnet" : "Configured Solana RPC",
            Provider = provider
        };
    }
    public async Task<object> PrepareLudcDeposit(string authToken, string senderWalletAddress, decimal amount)
    {
        var isValid = await ValidateSession(authToken, "Player");
        if (!isValid) return null;

        if (string.IsNullOrWhiteSpace(senderWalletAddress) || amount <= 0)
            return null;

        var playerIdStr = _utilService.Decrypt(authToken);
        if (!int.TryParse(playerIdStr, out int playerId))
            return null;

        using var ctx = _contextFactory.CreateDbContext();
        var wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == playerId && w.AddressType == "LUDC");
        if (wallet == null || string.IsNullOrWhiteSpace(wallet.WalletAddress))
            return null;

        return await _ludcPaymentProvider.PrepareDepositFromExternalWalletAsync(senderWalletAddress, wallet.WalletAddress, amount);
    }
    public async Task<bool> ConfirmSolanaTransaction(string authToken, string requiredRole, string signature)
    {
        var isValid = await ValidateSession(authToken, requiredRole);
        if (!isValid || string.IsNullOrWhiteSpace(signature))
            return false;

        return await _ludcPaymentProvider.ConfirmSignatureAsync(signature);
    }
    public async Task<object> PrepareAssetSwap(string authToken, string senderWalletAddress, string inputAsset, string outputAsset, decimal amount, int slippageBps = 100)
    {
        try
        {
            var isValid = await ValidateSession(authToken, "Player");
            if (!isValid)
                return new { Success = false, Error = "Unauthorized." };

            if (string.IsNullOrWhiteSpace(senderWalletAddress) || amount <= 0)
                return new { Success = false, Error = "Invalid swap request." };

            var playerIdStr = _utilService.Decrypt(authToken);
            if (!int.TryParse(playerIdStr, out int playerId))
                return new { Success = false, Error = "Invalid session." };

            using var ctx = _contextFactory.CreateDbContext();
            var wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == playerId && w.AddressType == "LUDC");
            if (wallet == null || string.IsNullOrWhiteSpace(wallet.WalletAddress))
                return new { Success = false, Error = "Internal LUDC wallet not ready." };

            var normalizedInput = (inputAsset ?? string.Empty).Trim().ToUpperInvariant();
            var normalizedOutput = (outputAsset ?? string.Empty).Trim().ToUpperInvariant();

            if (normalizedInput == normalizedOutput)
                return new { Success = false, Error = "Choose two different assets." };

            if (normalizedInput != "LUDC" && normalizedOutput != "LUDC")
                return new { Success = false, Error = "One side of the swap must be LUDC." };

            var inputConfig = GetSupportedAsset(normalizedInput, _ludcPaymentProvider.MintAddress);
            var outputConfig = GetSupportedAsset(normalizedOutput, _ludcPaymentProvider.MintAddress);
            if (inputConfig == null || outputConfig == null)
                return new { Success = false, Error = "Unsupported asset." };

            if (slippageBps <= 0 || slippageBps > MaxSwapSlippageBps)
                slippageBps = 100;

            decimal scale = (decimal)Math.Pow(10, inputConfig.Value.Decimals);
            ulong amountRaw = Convert.ToUInt64(decimal.Round(amount * scale, 0, MidpointRounding.AwayFromZero));
            if (amountRaw == 0)
                return new { Success = false, Error = "Swap amount is too small." };

            string receiver;
            if (normalizedOutput == "LUDC")
            {
                receiver = wallet.WalletAddress;
            }
            else if (!string.IsNullOrEmpty(outputConfig.Value.TokenProgram))
            {
                receiver = DeriveAssociatedTokenAddress(senderWalletAddress, outputConfig.Value.Mint, outputConfig.Value.TokenProgram);
            }
            else
            {
                receiver = senderWalletAddress;
            }

            using var order = await _jupiterSwapService.GetOrderAsync(
                inputConfig.Value.Mint,
                outputConfig.Value.Mint,
                amountRaw.ToString(),
                senderWalletAddress,
                receiver,
                slippageBps);

            var root = order.RootElement;
            var requestId = GetJsonString(root, "requestId");
            var transaction = GetJsonString(root, "transaction");
            var inAmount = GetJsonString(root, "inAmount", amountRaw.ToString());
            var outAmount = GetJsonString(root, "outAmount", "0");
            var outUsdValue = GetJsonString(root, "outUsdValue");
            var priceImpactPct = GetJsonString(root, "priceImpactPct");
            var router = GetJsonString(root, "router");
            var error = GetJsonString(root, "error");
            var errorMessage = GetJsonString(root, "errorMessage");

            if (!string.IsNullOrWhiteSpace(error) || !string.IsNullOrWhiteSpace(errorMessage))
            {
                var swapError = string.IsNullOrWhiteSpace(errorMessage) ? error : errorMessage;
                if (string.Equals(swapError, "Insufficient funds", StringComparison.OrdinalIgnoreCase))
                {
                    swapError = normalizedInput == "USDC"
                        ? "Insufficient funds in Phantom wallet. You need enough USDC and a small SOL balance for fees."
                        : $"Insufficient {normalizedInput} in Phantom wallet.";
                }
                else if (string.Equals(swapError, "Failed to get quotes", StringComparison.OrdinalIgnoreCase))
                {
                    swapError = "No swap route is available for this amount right now. Try a smaller amount.";
                }
                return new { Success = false, Error = swapError };
            }

            if (string.IsNullOrWhiteSpace(requestId) || string.IsNullOrWhiteSpace(transaction))
                return new { Success = false, Error = "Jupiter did not return a signable swap transaction." };

            return new
            {
                Success = true,
                InputAsset = normalizedInput,
                InputMint = inputConfig.Value.Mint,
                InputAmount = amount,
                InputAmountRaw = inAmount,
                OutputAsset = normalizedOutput,
                OutputMint = outputConfig.Value.Mint,
                OutputAmountRaw = outAmount,
                Receiver = receiver,
                RequestId = requestId,
                Transaction = transaction,
                SlippageBps = slippageBps,
                Router = router,
                PriceImpactPct = priceImpactPct,
                OutUsdValue = outUsdValue
            };
        }
        catch (Exception ex)
        {
            return new { Success = false, Error = ex.Message };
        }
    }
    public async Task<object> ExecutePreparedSwap(string authToken, string requestId, string signedTransactionBase64)
    {
        try
        {
            var isValid = await ValidateSession(authToken, "Player");
            if (!isValid)
                return new { Success = false, Error = "Unauthorized." };

            if (string.IsNullOrWhiteSpace(requestId) || string.IsNullOrWhiteSpace(signedTransactionBase64))
                return new { Success = false, Error = "Invalid signed transaction." };

            using var result = await _jupiterSwapService.ExecuteOrderAsync(requestId, signedTransactionBase64);
            var root = result.RootElement;
            var signature = GetJsonString(root, "signature");
            var status = GetJsonString(root, "status");
            var slot = GetJsonString(root, "slot");
            var code = GetJsonString(root, "code");
            var error = GetJsonString(root, "error");
            var message = GetJsonString(root, "message");

            if (!string.IsNullOrWhiteSpace(error) || !string.IsNullOrWhiteSpace(message))
                return new { Success = false, Error = string.IsNullOrWhiteSpace(message) ? error : message };

            return new
            {
                Success = true,
                Signature = signature,
                Status = status,
                Slot = slot,
                Code = code
            };
        }
        catch (Exception ex)
        {
            return new { Success = false, Error = ex.Message };
        }
    }
    public async Task<object> GetSwapBalances(string authToken, string senderWalletAddress)
    {
        try
        {
            var isValid = await ValidateSession(authToken, "Player");
            if (!isValid)
                return new { Success = false, Error = "Unauthorized." };

            if (string.IsNullOrWhiteSpace(senderWalletAddress))
                return new { Success = false, Error = "Connect Phantom first." };

            var playerIdStr = _utilService.Decrypt(authToken);
            if (!int.TryParse(playerIdStr, out int playerId))
                return new { Success = false, Error = "Invalid session." };

            using var ctx = _contextFactory.CreateDbContext();
            var internalWallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == playerId && w.AddressType == "LUDC");
            var internalLudc = internalWallet?.AvailableBalance ?? 0m;

            var phantomSol = await _solanaRpc.GetBalanceAsync(senderWalletAddress, Commitment.Confirmed);
            var phantomSolAmount = phantomSol.WasSuccessful && phantomSol.Result != null
                ? phantomSol.Result.Value / 1_000_000_000m
                : 0m;

            var phantomUsdc = await GetTokenBalanceAsync(senderWalletAddress, UsdcMint, StandardTokenProgram);
            var phantomLudc = await GetTokenBalanceAsync(senderWalletAddress, _ludcPaymentProvider.MintAddress, Token2022Program);

            return new
            {
                Success = true,
                PhantomSol = phantomSolAmount,
                PhantomUsdc = phantomUsdc,
                PhantomLudc = phantomLudc,
                InternalLudc = internalLudc
            };
        }
        catch (Exception ex)
        {
            return new { Success = false, Error = ex.Message };
        }
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

            var manualWithdrawals = await ctx.CashWithdrawals
                .Where(w => w.PlayerId == playerId)
                .OrderByDescending(w => w.CreatedDate)
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
                ManualWithdrawals = manualWithdrawals,
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

            if (result != "ERROR" &&
                !result.Contains("INSUFFICIENT", StringComparison.OrdinalIgnoreCase) &&
                result.StartsWith("internal:withdraw:", StringComparison.OrdinalIgnoreCase))
            {
                var recipientPlayerId = await ResolveInternalRecipientPlayerIdAsync(destinationAddress);
                if (recipientPlayerId.HasValue)
                {
                    var recipient = await ctx.Players.FirstOrDefaultAsync(p => p.PlayerId == recipientPlayerId.Value);
                    if (recipient != null)
                        await Clients.User(recipient.PlayerId.ToString()).SendAsync("PlayerInfoUpdate", await _utilService.CastPlayerToInfoAsync(recipient));
                }
            }
            
            return result;
        }
        catch (Exception ex) { return "Error: " + ex.Message; }
    }

    private async Task<int?> ResolveInternalRecipientPlayerIdAsync(string destination)
    {
        var normalizedDestination = destination?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedDestination))
            return null;

        using var ctx = _contextFactory.CreateDbContext();
        var walletPlayerId = await ctx.PlayerWallet
            .Where(w => w.WalletAddress == normalizedDestination)
            .Select(w => (int?)w.PlayerId)
            .FirstOrDefaultAsync();

        if (walletPlayerId.HasValue)
            return walletPlayerId.Value;

        return await ctx.PlayerWalletKey
            .Where(k => k.PublicKey == normalizedDestination)
            .Select(k => (int?)k.PlayerId)
            .FirstOrDefaultAsync();
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
            var ludoHubContext = (IHubContext<Ludo.Api.Hubs.LudoHub>)Context.GetHttpContext().RequestServices.GetService(typeof(IHubContext<Ludo.Api.Hubs.LudoHub>));
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

    public async Task<string> SubmitManualDeposit(int playerId, decimal amount, string method, string referenceNumber, string receiptUrl)
    {
        try
        {
            using var ctx = _contextFactory.CreateDbContext();
            string normalizedReference = (referenceNumber ?? string.Empty).Trim().ToUpperInvariant();
            string normalizedReceipt = (receiptUrl ?? string.Empty).Trim();
            Console.WriteLine($"[DashboardHub] SubmitManualDeposit payload: playerId={playerId}, amount={amount}, method={method}, reference={normalizedReference}, receiptLen={normalizedReceipt.Length}, receiptPrefix={(normalizedReceipt.Length > 32 ? normalizedReceipt[..32] : normalizedReceipt)}");

            if (amount <= 0)
                return "Invalid amount.";

            if (string.IsNullOrWhiteSpace(normalizedReference))
                return "Reference number is required.";

            if (string.IsNullOrWhiteSpace(normalizedReceipt))
                return "Receipt proof is required.";

            bool duplicateExists = await ctx.CashDeposits.AnyAsync(d =>
                d.ReferenceNumber == normalizedReference ||
                d.ReceiptImageUrl == normalizedReceipt);

            if (duplicateExists)
                return "Duplicate receipt or reference number.";

            var deposit = new CashDeposit
            {
                PlayerId = playerId,
                Amount = amount,
                ReferenceNumber = normalizedReference,
                PaymentMethod = method,
                ReceiptImageUrl = normalizedReceipt,
                Status = "Pending",
                CreatedDate = DateTime.UtcNow
            };

            ctx.CashDeposits.Add(deposit);
            await ctx.SaveChangesAsync();
            Console.WriteLine($"[DashboardHub] SubmitManualDeposit saved: depositId={deposit.Id}, receiptLen={deposit.ReceiptImageUrl?.Length ?? 0}");
            return "Success";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error submitting deposit: {ex.Message}");
            return "Error";
        }
    }

    public async Task<string> SubmitManualWithdrawal(string authToken, decimal amount, string method, string destinationDetails)
    {
        try
        {
            var player = await GetAuthorizedPlayer(authToken);
            if (player == null) return "Unauthorized.";

            if (amount <= 0)
                return "Invalid amount.";

            string normalizedMethod = (method ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedMethod) || normalizedMethod == "Select Payout Method")
                return "Select a payout method.";

            string normalizedDestination = (destinationDetails ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedDestination))
                return "Account details are required.";

            using var ctx = _contextFactory.CreateDbContext();
            var wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == player.PlayerId && w.AddressType == "LUDC");
            if (wallet == null) return "Wallet Not Found";
            if (wallet.AvailableBalance < amount) return "Insufficient internal balance.";

            bool duplicatePending = await ctx.CashWithdrawals.AnyAsync(w =>
                w.PlayerId == player.PlayerId &&
                w.Status == "Pending" &&
                w.Amount == amount &&
                w.PayoutMethod == normalizedMethod &&
                w.DestinationDetails == normalizedDestination);

            if (duplicatePending)
                return "A similar payout request is already pending.";

            var withdrawal = new CashWithdrawal
            {
                PlayerId = player.PlayerId,
                Amount = amount,
                PayoutMethod = normalizedMethod,
                DestinationDetails = normalizedDestination,
                Status = "Pending",
                CreatedDate = DateTime.UtcNow
            };

            ctx.CashWithdrawals.Add(withdrawal);
            await ctx.SaveChangesAsync();
            return "Success";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error submitting withdrawal: {ex.Message}");
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
                    ReferenceNumber = d.ReferenceNumber,
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

    public async Task<List<object>> GetPendingWithdrawals()
    {
        try
        {
            using var ctx = _contextFactory.CreateDbContext();
            var withdrawals = await ctx.CashWithdrawals
                .Include(w => w.Player)
                .Where(w => w.Status == "Pending")
                .OrderByDescending(w => w.CreatedDate)
                .Select(w => new
                {
                    Id = w.Id,
                    PlayerName = w.Player.Name,
                    playerId = w.PlayerId,
                    Amount = w.Amount,
                    Method = w.PayoutMethod,
                    DestinationDetails = w.DestinationDetails,
                    Date = w.CreatedDate
                })
                .ToListAsync<object>();
            return withdrawals;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting pending withdrawals: {ex.Message}");
            return new List<object>();
        }
    }

    public async Task<string> ProcessDeposit(string authToken, int depositId, string action, string note)
    {
        try
        {
            var admin = await GetAuthorizedAdmin(authToken);
            if (admin == null) return "Unauthorized.";

            var normalizedAction = (action ?? string.Empty).Trim();
            if (normalizedAction != "Approved" && normalizedAction != "Rejected")
                return "Invalid action.";

            using var ctx = _contextFactory.CreateDbContext();
            var deposit = await ctx.CashDeposits.FirstOrDefaultAsync(d => d.Id == depositId);
            if (deposit == null || deposit.Status != "Pending") return "Not Found";

            if (normalizedAction == "Approved")
            {
                var wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == deposit.PlayerId);
                if (wallet == null) return "Wallet Not Found";

                wallet.AvailableBalance += deposit.Amount;

                var transaction = new WalletTransaction
                {
                    PlayerId = deposit.PlayerId,
                    Amount = deposit.Amount,
                    Type = TransactionType.Deposit,
                    Status = WalletTransactionStatus.Completed,
                    Description = $"Approved {deposit.PaymentMethod} deposit ({deposit.ReferenceNumber}). {note}",
                    OperationId = Guid.NewGuid(),
                    BalanceAfter = wallet.AvailableBalance
                };

                ctx.WalletTransaction.Add(transaction);
                ctx.PlayerWallet.Update(wallet);
            }

            deposit.Status = normalizedAction;
            deposit.AdminNote = note;
            deposit.ProcessedByAdminId = admin.PlayerId;
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

    public async Task<string> ProcessWithdrawal(string authToken, int withdrawalId, string action, string note)
    {
        try
        {
            var admin = await GetAuthorizedAdmin(authToken);
            if (admin == null) return "Unauthorized.";

            var normalizedAction = (action ?? string.Empty).Trim();
            if (normalizedAction != "Approved" && normalizedAction != "Rejected")
                return "Invalid action.";

            using var ctx = _contextFactory.CreateDbContext();
            var withdrawal = await ctx.CashWithdrawals.FirstOrDefaultAsync(w => w.Id == withdrawalId);
            if (withdrawal == null || withdrawal.Status != "Pending") return "Not Found";

            if (normalizedAction == "Approved")
            {
                var wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == withdrawal.PlayerId && w.AddressType == "LUDC");
                if (wallet == null) return "Wallet Not Found";
                if (wallet.AvailableBalance < withdrawal.Amount) return "Insufficient balance.";

                wallet.AvailableBalance -= withdrawal.Amount;

                var transaction = new WalletTransaction
                {
                    PlayerId = withdrawal.PlayerId,
                    Amount = withdrawal.Amount,
                    Type = TransactionType.Withdrawal,
                    Status = WalletTransactionStatus.Completed,
                    Description = $"Approved manual {withdrawal.PayoutMethod} payout. {note}",
                    OperationId = Guid.NewGuid(),
                    BalanceAfter = wallet.AvailableBalance
                };

                ctx.WalletTransaction.Add(transaction);
                ctx.PlayerWallet.Update(wallet);
            }

            withdrawal.Status = normalizedAction;
            withdrawal.AdminNote = note;
            withdrawal.ProcessedByAdminId = admin.PlayerId;
            withdrawal.ProcessedDate = DateTime.UtcNow;

            ctx.CashWithdrawals.Update(withdrawal);
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
