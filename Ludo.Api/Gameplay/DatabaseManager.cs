using LudoServer.Data;
using LudoServer.Models;
using Microsoft.EntityFrameworkCore;
using SignalR.Server.Payments;
using SignalR.Server.Services;
using System.Collections.Concurrent;

namespace SignalR.Server
{
    public class DatabaseManager(IDbContextFactory<LudoDbContext> _contextFactory, CryptoHelper _crypto, UtilService _utilService)
    {
        public ConcurrentDictionary<string, GameRoom> _gameRooms { get; set; } = new();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _roomLocks = new();
        private sealed record BotPersona(string Name, string Slug, string PictureUrl, string City, string Country);

        private static readonly BotPersona[] BotPersonas =
        [
            new("Aria Blaze", "aria.blaze", "avatar_aria_blaze.svg", "Phoenix", "United States"),
            new("Mika Stone", "mika.stone", "avatar_mika_stone.svg", "Austin", "United States"),
            new("Zara Vale", "zara.vale", "avatar_zara_vale.svg", "London", "United Kingdom"),
            new("Rayan Knox", "rayan.knox", "avatar_rayan_knox.svg", "Toronto", "Canada"),
            new("Nia Frost", "nia.frost", "avatar_nia_frost.svg", "Berlin", "Germany"),
            new("Kairo Vega", "kairo.vega", "avatar_kairo_vega.svg", "Dubai", "United Arab Emirates"),
            new("Lina Quest", "lina.quest", "avatar_lina_quest.svg", "Singapore", "Singapore"),
            new("Omar Flint", "omar.flint", "avatar_omar_flint.svg", "Doha", "Qatar"),
            new("Tara Moon", "tara.moon", "avatar_tara_moon.svg", "Sydney", "Australia"),
            new("Ilyas Ray", "ilyas.ray", "avatar_ilyas_ray.svg", "Karachi", "Pakistan"),
            new("Mina Spark", "mina.spark", "avatar_mina_spark.svg", "Lahore", "Pakistan"),
            new("Sami Crown", "sami.crown", "avatar_sami_crown.svg", "Riyadh", "Saudi Arabia"),
            new("Noor Atlas", "noor.atlas", "avatar_noor_atlas.svg", "Muscat", "Oman"),
            new("Vikram Ace", "vikram.ace", "avatar_vikram_ace.svg", "Delhi", "India"),
            new("Hana Drift", "hana.drift", "avatar_hana_drift.svg", "Kuala Lumpur", "Malaysia"),
            new("Leo Rune", "leo.rune", "avatar_leo_rune.svg", "Lisbon", "Portugal")
        ];

        private async Task BroadcastMatchUpdate()
        {
            await Task.CompletedTask;
        }

        public async Task<List<Player>> EnsureBotPlayersAsync(int desiredCount, decimal walletFloat, string avatarBaseUrl)
        {
            using var ctx = _contextFactory.CreateDbContext();
            var bots = await ctx.Players
                .Where(p => p.Role == "Bot")
                .OrderBy(p => p.PlayerId)
                .ToListAsync();

            for (int i = 0; i < desiredCount; i++)
            {
                var persona = BotPersonas[i % BotPersonas.Length];
                var bot = i < bots.Count ? bots[i] : new Player();
                var personaNumber = i / BotPersonas.Length;

                bot.Name = personaNumber == 0 ? persona.Name : $"{persona.Name} {personaNumber + 1}";
                bot.Email = personaNumber == 0
                    ? $"{persona.Slug}@ludocities.local"
                    : $"{persona.Slug}{personaNumber + 1}@ludocities.local";
                bot.PictureUrl = $"{avatarBaseUrl.TrimEnd('/')}/{persona.PictureUrl}";
                bot.City = persona.City;
                bot.Country = persona.Country;
                bot.Role = "Bot";
                bot.IsActive = true;
                bot.IsOnline = true;
                bot.CreatedDate = bot.CreatedDate == default ? DateTime.UtcNow : bot.CreatedDate;

                if (string.IsNullOrWhiteSpace(bot.AuthToken) || bot.AuthToken.StartsWith("bot-", StringComparison.OrdinalIgnoreCase))
                    bot.AuthToken = $"player-{Guid.NewGuid():N}";

                if (i >= bots.Count)
                {
                    ctx.Players.Add(bot);
                    bots.Add(bot);
                }
            }

            await ctx.SaveChangesAsync();

            foreach (var bot in bots.Take(desiredCount))
            {
                var wallet = await _crypto.EnsurePlayerWalletExists(bot.PlayerId, CurrencyType.LUDC);
                if (wallet.AvailableBalance < walletFloat)
                {
                    var topUp = walletFloat - wallet.AvailableBalance;
                    await _crypto.OffChainTransaction(bot.PlayerId, topUp, "Bot Float Seed", "", false, "", TransactionType.Deposit);
                }
            }

            return bots.Take(desiredCount).ToList();
        }

        public async Task SeedBotRoomsAsync(int targetRoomCount, IReadOnlyList<decimal> betAmounts, IReadOnlyList<string> gameTypes)
        {
            if (targetRoomCount <= 0 || betAmounts.Count == 0 || gameTypes.Count == 0)
                return;

            using var ctx = _contextFactory.CreateDbContext();
            var botIds = await ctx.Players
                .Where(p => p.Role == "Bot" && p.IsActive && !p.IsBlocked)
                .Select(p => p.PlayerId)
                .ToListAsync();
            if (botIds.Count == 0)
                return;

            var activeBotRoomCount = await ctx.Games
                .CountAsync(g => g.State == "Active" &&
                                 !g.IsPrivate &&
                                 !g.IsPractice &&
                                 g.TournamentId == null &&
                                 g.Owner.HasValue &&
                                 botIds.Contains(g.Owner.Value));

            for (int i = activeBotRoomCount; i < targetRoomCount; i++)
            {
                var bot = await PickAvailableBotAsync(ctx, botIds);
                if (bot == null)
                    return;

                var gameType = gameTypes[i % gameTypes.Count];
                var bet = betAmounts[i % betAmounts.Count];
                var gameDto = new SharedCode.GameDto
                {
                    GameType = gameType,
                    PlayerCount = ParseGameType(gameType),
                    BetAmount = bet,
                    IsPrivateGame = false,
                    IsPracticeGame = false,
                    IsTournamentGame = false
                };

                await JoinGameLobby(bot, gameDto);
            }
        }

        public async Task FillExpiredRoomsWithBotsAsync(TimeSpan waitBeforeFill)
        {
            using var ctx = _contextFactory.CreateDbContext();
            var botIds = await ctx.Players
                .Where(p => p.Role == "Bot" && p.IsActive && !p.IsBlocked)
                .Select(p => p.PlayerId)
                .ToListAsync();
            if (botIds.Count == 0)
                return;

            var cutoff = DateTime.UtcNow - waitBeforeFill;
            var waitingRooms = await ctx.Games
                .Include(g => g.MultiPlayer)
                .Where(g => g.State == "Active" &&
                            !g.IsPrivate &&
                            !g.IsPractice &&
                            g.TournamentId == null &&
                            g.CreatedDate <= cutoff)
                .OrderBy(g => g.CreatedDate)
                .ToListAsync();

            foreach (var game in waitingRooms)
            {
                var seatedIds = GetSeatIds(game.MultiPlayer).ToList();
                var hasHuman = seatedIds.Any(id => !botIds.Contains(id));
                if (!hasHuman)
                    continue;

                var requiredPlayers = ParseGameType(game.GameType);
                while (seatedIds.Count < requiredPlayers)
                {
                    var bot = await PickAvailableBotAsync(ctx, botIds, seatedIds);
                    if (bot == null)
                        break;

                    var added = await AddBotToExistingGameAsync(game.GameId, bot.PlayerId);
                    if (!added)
                        break;

                    seatedIds.Add(bot.PlayerId);
                }

                if (seatedIds.Count >= requiredPlayers)
                    await TryStartRoomAsync(game.RoomCode);
            }
        }

        public async Task<Game> JoinGameLobby(Player player, SharedCode.GameDto gameDTO)
        {
            Console.WriteLine("Join " + DateTime.UtcNow);
            using var ctx = _contextFactory.CreateDbContext();
            int? ParsedId = null;
            Game existingGame = null;

            if (gameDTO.IsTournamentGame)
            {
                if (!int.TryParse(gameDTO.RoomCode, out int parsedId))
                    throw new ArgumentException("Invalid tournament ID format in RoomCode.");
                ParsedId = parsedId;

                var tournament = await ctx.Tournaments.FirstOrDefaultAsync(tc => tc.TournamentId == ParsedId);
                if (tournament == null)
                    throw new Exception($"Tournament {ParsedId} not found.");

                var challenger = await ctx.TournamentChallengers.FirstOrDefaultAsync(tc =>
                    tc.TournamentId == ParsedId &&
                    tc.PlayerId == player.PlayerId &&
                    tc.Status == "JOINED");
                if (challenger == null)
                    throw new Exception("Player is not joined in this tournament.");

                gameDTO.BetAmount = tournament.EntryFee;
                gameDTO.RoomCode = "";
                existingGame = ctx.Games.Include(g => g.MultiPlayer).FirstOrDefault(g => g.TournamentId == ParsedId && g.State == "Active");
            }
            else if (gameDTO.IsPracticeGame)
                existingGame = await ctx.Games.Include(g => g.MultiPlayer).FirstOrDefaultAsync(g => g.GameType == gameDTO.GameType && g.BetAmount == 0 && g.State == "Active");
            else
                existingGame = await ctx.Games.Include(g => g.MultiPlayer).FirstOrDefaultAsync(g => g.RoomCode == gameDTO.RoomCode && g.State == "Active");

            if (existingGame == null)
            {
                // Create unique room code
                string roomCode;
                do
                {
                    roomCode = Random.Shared.Next(10000000, 99999999).ToString();
                }
                while (await ctx.Games.AnyAsync(g => g.RoomCode == roomCode));
                
                gameDTO.RoomCode = roomCode;

                // Deduct game fee
                if (!gameDTO.IsPracticeGame && !gameDTO.IsTournamentGame) // 🛑 FIX: Skip deduction for tournaments
                {
                    bool deducted = await _crypto.deductGameFee(player.PlayerId, ParsedId, gameDTO.RoomCode, gameDTO.IsTournamentGame, gameDTO.BetAmount);
                    if (!deducted)
                    {
                        Console.WriteLine($"Game fee FAILED TO deduct for player {player.PlayerId} in room {gameDTO.RoomCode}.");
                        return null;
                    }
                }

                MultiPlayer multiPlayer = GetGamePlayers(player.PlayerId, null, ctx);
                multiPlayer.RoomCode = int.Parse(roomCode);

                existingGame = new Game
                {
                    PlayerCount = gameDTO.PlayerCount,
                    GameType = gameDTO.GameType,
                    BetAmount = gameDTO.BetAmount,
                    RoomCode = gameDTO.RoomCode,
                    IsPrivate = gameDTO.IsPrivateGame,
                    IsPractice = gameDTO.IsPracticeGame,
                    TournamentId = gameDTO.IsTournamentGame ? ParsedId : null,
                    Owner = player.PlayerId,
                    State = "Active",
                    MultiPlayer = multiPlayer
                };

                ctx.Games.Add(existingGame);
                await ctx.SaveChangesAsync();
            }
            else
            {
                if (!existingGame.IsPractice && !gameDTO.IsTournamentGame) // 🛑 FIX: Skip deduction for tournaments
                {
                    if (!await _crypto.deductGameFee(player.PlayerId, existingGame.TournamentId, existingGame.RoomCode, gameDTO.IsTournamentGame, existingGame.BetAmount))
                    {
                        Console.WriteLine($"Game fee FAILED TO deduct for player {player.PlayerId} in room {existingGame.RoomCode}.");
                        return null;
                    }
                }
                // Join existing game
                GetGamePlayers(player.PlayerId, existingGame.MultiPlayer, ctx);
                await ctx.SaveChangesAsync();
            }

            await EnsureGameRoomUsersAsync(existingGame, ctx, gameDTO);

            // 🛑 FIX: Broadcast AFTER the user has been added to memory
            await BroadcastMatchUpdate(); 

            return existingGame;
        }

        public async Task<(Game game, User user)> LeaveGameLobby(int playerId)
        {
            using var ctx = _contextFactory.CreateDbContext();

            Game existingGame =
                await GetActiveGameAsync("Active", playerId, ctx) ??
                await GetActiveGameAsync("Playing", playerId, ctx);

            if (existingGame == null)
                return (null, null);

            var roomLock = GetRoomLock(existingGame.RoomCode);
            await roomLock.WaitAsync();
            try
            {
                // 🔒 Reload inside lock
                existingGame =
                    await GetActiveGameAsync("Active", playerId, ctx) ??
                    await GetActiveGameAsync("Playing", playerId, ctx);

                if (existingGame == null)
                    return (null, null);

                bool isEmpty = false;
                // ✅ Refund ONLY if game was still Active
                if (existingGame.State == "Active")
                {
                    var multiPlayer = existingGame.MultiPlayer;

                    if (multiPlayer.P1 == playerId) multiPlayer.P1 = null;
                    else if (multiPlayer.P2 == playerId) multiPlayer.P2 = null;
                    else if (multiPlayer.P3 == playerId) multiPlayer.P3 = null;
                    else if (multiPlayer.P4 == playerId) multiPlayer.P4 = null;

                    isEmpty =
                        multiPlayer.P1 == null &&
                        multiPlayer.P2 == null &&
                        multiPlayer.P3 == null &&
                        multiPlayer.P4 == null;

                    if (isEmpty)
                        existingGame.State = "Terminated";
                    await ctx.SaveChangesAsync();
                    if (!existingGame.IsPractice && existingGame.TournamentId == null)
                    {
                        try
                        {
                            // 🛑 FIX: Passing roomCode as the 6th parameter
                            await _crypto.OffChainTransaction(playerId, existingGame.BetAmount, "Game Refund", "", false, existingGame.RoomCode, TransactionType.Deposit);
                            Console.WriteLine($"Refunded {existingGame.BetAmount} to player {playerId} for game {existingGame.RoomCode}.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Refund failed for player {playerId} in game {existingGame.RoomCode}: {ex.Message}");
                        }
                    }
                }
                
                User user = null;

                if (_gameRooms.TryGetValue(existingGame.RoomCode, out GameRoom room))
                    user = await room.PlayerLeft(playerId);

                if (isEmpty)
                {
                    _gameRooms.TryRemove(existingGame.RoomCode, out _);
                    await BroadcastMatchUpdate(); // Notify Admin Dashboard
                }
                return (existingGame, user);
            }
            finally
            {
                roomLock.Release();
            }
        }
        private SemaphoreSlim GetRoomLock(string roomCode)
        {
            return _roomLocks.GetOrAdd(roomCode, _ => new SemaphoreSlim(1, 1));
        }
        private void RemoveRoomLock(string roomCode)
        {
            _roomLocks.TryRemove(roomCode, out _);
        }
        internal async Task<(Game existingGame, List<SharedCode.PlayerDto> seats, string rollsString)> Ready(int playerId, string roomCode)
        {
            using var ctx = _contextFactory.CreateDbContext();

            Game existingGame = await GetActiveGameAsync("Active", playerId, ctx, roomCode);
            if (existingGame == null)
                return (null, null, null);

            var roomLock = GetRoomLock(existingGame.RoomCode);
            await roomLock.WaitAsync();

            try
            {
                // Reload inside lock
                existingGame = await GetActiveGameAsync("Active", playerId, ctx, roomCode);
                if (existingGame == null)
                    return (null, null, null);

                if (existingGame.State == "Playing")
                    return (existingGame, null, "");

                var (seats, playerList) = await BuildSeats(existingGame, ctx);

                int requiredPlayers = ParseGameType(existingGame.GameType);

                if (seats.Count == requiredPlayers)
                {
                    existingGame.State = "Playing";
                    await ctx.SaveChangesAsync();

                    if (_gameRooms.TryGetValue(existingGame.RoomCode, out GameRoom room))
                    {
                        if (room.engine == null)
                        {
                            for (int i = 0; i < room.Users.Count; i++)
                            {
                                room.Users[i].PlayerColor = seats[i].PlayerColor.ToLower();
                                room.Users[i].player = playerList[i];
                            }
                            room.InitializeEngine(seats);
                        }
                        return (existingGame, seats, room.engine.EngineHelper.rollsString);
                    }
                }
                return (existingGame, seats, "");
            }
            finally
            {
                roomLock.Release();
            }
        }

        internal async Task<(Game? game, List<SharedCode.PlayerDto>? seats, string rollsString, bool started)> TryStartRoomAsync(string roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode))
                return (null, null, string.Empty, false);

            using var ctx = _contextFactory.CreateDbContext();
            var roomLock = GetRoomLock(roomCode);
            await roomLock.WaitAsync();

            try
            {
                var existingGame = await ctx.Games
                    .Include(g => g.MultiPlayer)
                    .FirstOrDefaultAsync(g =>
                        g.RoomCode == roomCode &&
                        (g.State == "Active" || g.State == "Playing"));
                if (existingGame == null)
                    return (null, null, string.Empty, false);

                var (seats, playerList) = await BuildSeats(existingGame, ctx);
                var requiredPlayers = ParseGameType(existingGame.GameType);
                if (seats.Count < requiredPlayers)
                    return (existingGame, seats, string.Empty, false);

                if (!string.Equals(existingGame.State, "Playing", StringComparison.OrdinalIgnoreCase))
                {
                    existingGame.State = "Playing";
                    await ctx.SaveChangesAsync();
                }

                var room = await EnsureGameRoomUsersAsync(existingGame, ctx);
                if (room == null)
                    return (existingGame, seats, string.Empty, false);

                if (room.engine == null)
                {
                    lock (room.Users)
                    {
                        foreach (var seat in seats)
                        {
                            var user = room.Users.FirstOrDefault(u => u.player?.PlayerId == seat.PlayerId);
                            var player = playerList.FirstOrDefault(p => p.PlayerId == seat.PlayerId);
                            if (user != null && player != null)
                            {
                                user.PlayerColor = seat.PlayerColor.ToLower();
                                user.player = player;
                            }
                        }
                    }

                    room.InitializeEngine(seats);
                }

                return (existingGame, seats, room.engine?.EngineHelper.rollsString ?? string.Empty, true);
            }
            finally
            {
                roomLock.Release();
            }
        }

        private async Task<Player?> PickAvailableBotAsync(LudoDbContext ctx, IReadOnlyCollection<int> botIds, IReadOnlyCollection<int>? excludeIds = null)
        {
            var excluded = excludeIds?.ToHashSet() ?? new HashSet<int>();
            var activeGames = await ctx.Games
                .Include(g => g.MultiPlayer)
                .Where(g => (g.State == "Active" || g.State == "Playing") && g.MultiPlayer != null)
                .Select(g => new { g.MultiPlayer.P1, g.MultiPlayer.P2, g.MultiPlayer.P3, g.MultiPlayer.P4 })
                .ToListAsync();

            var activeBotIds = activeGames
                .SelectMany(g => new[] { g.P1, g.P2, g.P3, g.P4 })
                .Where(id => id.HasValue && botIds.Contains(id.Value))
                .Select(id => id.Value)
                .ToHashSet();

            var activeBotIdList = activeBotIds.ToList();
            var excludedIdList = excluded.ToList();
            var candidates = await ctx.Players
                .Where(p => p.Role == "Bot" &&
                            p.IsActive &&
                            !p.IsBlocked &&
                            botIds.Contains(p.PlayerId) &&
                            !activeBotIdList.Contains(p.PlayerId) &&
                            !excludedIdList.Contains(p.PlayerId))
                .ToListAsync();

            return candidates.Count == 0 ? null : candidates[Random.Shared.Next(candidates.Count)];
        }

        private async Task<bool> AddBotToExistingGameAsync(int gameId, int botPlayerId)
        {
            using var ctx = _contextFactory.CreateDbContext();
            var existingGame = await ctx.Games
                .Include(g => g.MultiPlayer)
                .FirstOrDefaultAsync(g => g.GameId == gameId && g.State == "Active");
            if (existingGame == null || string.IsNullOrWhiteSpace(existingGame.RoomCode))
                return false;

            var roomLock = GetRoomLock(existingGame.RoomCode);
            await roomLock.WaitAsync();

            try
            {
                existingGame = await ctx.Games
                    .Include(g => g.MultiPlayer)
                    .FirstOrDefaultAsync(g => g.GameId == gameId && g.State == "Active");
                if (existingGame == null)
                    return false;

                var bot = await ctx.Players.FirstOrDefaultAsync(p =>
                    p.PlayerId == botPlayerId &&
                    p.Role == "Bot" &&
                    p.IsActive &&
                    !p.IsBlocked);
                if (bot == null)
                    return false;

                var seatedIds = GetSeatIds(existingGame.MultiPlayer).ToList();
                if (seatedIds.Contains(bot.PlayerId) || seatedIds.Count >= ParseGameType(existingGame.GameType))
                    return false;

                if (!existingGame.IsPractice && existingGame.TournamentId == null && existingGame.BetAmount > 0)
                {
                    var deducted = await _crypto.deductGameFee(bot.PlayerId, existingGame.TournamentId, existingGame.RoomCode, false, existingGame.BetAmount);
                    if (!deducted)
                        return false;
                }

                GetGamePlayers(bot.PlayerId, existingGame.MultiPlayer, ctx);
                await ctx.SaveChangesAsync();
                await EnsureGameRoomUsersAsync(existingGame, ctx);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            finally
            {
                roomLock.Release();
            }
        }

        private static IEnumerable<int> GetSeatIds(MultiPlayer? multiPlayer)
        {
            if (multiPlayer == null)
                yield break;

            if (multiPlayer.P1.HasValue) yield return multiPlayer.P1.Value;
            if (multiPlayer.P2.HasValue) yield return multiPlayer.P2.Value;
            if (multiPlayer.P3.HasValue) yield return multiPlayer.P3.Value;
            if (multiPlayer.P4.HasValue) yield return multiPlayer.P4.Value;
        }

        private async Task<GameRoom?> EnsureGameRoomUsersAsync(Game? existingGame, LudoDbContext ctx, SharedCode.GameDto? gameDto = null)
        {
            if (existingGame == null || string.IsNullOrWhiteSpace(existingGame.RoomCode))
                return null;

            gameDto ??= BuildGameDto(existingGame);
            gameDto.RoomCode = existingGame.RoomCode;

            var gameRoom = _gameRooms.GetOrAdd(existingGame.RoomCode, _ => new GameRoom(_contextFactory, this, _crypto, _utilService, gameDto));
            var seatedIds = GetSeatIds(existingGame.MultiPlayer).ToList();
            if (seatedIds.Count == 0)
                return gameRoom;

            var players = await ctx.Players
                .Where(p => seatedIds.Contains(p.PlayerId))
                .ToListAsync();

            lock (gameRoom.Users)
            {
                foreach (var player in players)
                {
                    if (!gameRoom.Users.Any(u => u.player?.PlayerId == player.PlayerId))
                        gameRoom.Users.Add(new User(player, "Color"));
                }
            }

            return gameRoom;
        }

        private SharedCode.GameDto BuildGameDto(Game existingGame)
        {
            return new SharedCode.GameDto
            {
                GameType = existingGame.GameType ?? "2",
                RoomCode = existingGame.RoomCode ?? string.Empty,
                BetAmount = existingGame.BetAmount,
                PlayerCount = existingGame.PlayerCount > 0 ? existingGame.PlayerCount : ParseGameType(existingGame.GameType),
                IsPrivateGame = existingGame.IsPrivate,
                IsPracticeGame = existingGame.IsPractice,
                IsTournamentGame = existingGame.TournamentId.HasValue
            };
        }

        private int ParseGameType(string gameType)
        {
            return gameType switch
            {
                "2" => 2,
                "4" => 4,
                "22" => 4,
                _ => int.TryParse(gameType, out int val) ? val : 4
            };
        }
        private async Task<(List<SharedCode.PlayerDto> seats, List<Player> playerList)> BuildSeats(Game existingGame, LudoDbContext ctx)
        {
            var playerSlots = new (int? PlayerId, string Color)[]{
                (existingGame.MultiPlayer.P1, "Red"),
                (existingGame.MultiPlayer.P2, existingGame.GameType == "2" ? "Yellow" : "Green"),
                (existingGame.MultiPlayer.P3, "Yellow"),
                (existingGame.MultiPlayer.P4, "Blue")};
            var playerIds = playerSlots.Where(slot => slot.PlayerId.HasValue).Select(slot => slot.PlayerId.Value).ToList();
            var players = await ctx.Players.Where(p => playerIds.Contains(p.PlayerId)).ToListAsync();

            List<SharedCode.PlayerDto> seats = new();
            List<Player> playerList = new();
            foreach (var (playerId, color) in playerSlots.Where(s => s.PlayerId.HasValue))
            {
                var playerSub = players.FirstOrDefault(p => p.PlayerId == playerId.Value);
                if (playerSub != null)
                {
                    playerList.Add(playerSub);
                    seats.Add(new SharedCode.PlayerDto
                    {
                        PlayerId = playerSub.PlayerId,
                        PlayerName = playerSub.Name,
                        PlayerPicture = playerSub.PictureUrl,
                        PlayerColor = color
                    });
                }
            }
            return (seats, playerList);
        }
        private MultiPlayer GetGamePlayers(int playerId, MultiPlayer multiPlayer, LudoDbContext ctx)
        {
            if (multiPlayer == null)
            {
                multiPlayer = new MultiPlayer { P1 = playerId };
                ctx.MultiPlayers.Add(multiPlayer);
                return multiPlayer;
            }

            if (multiPlayer.P1 == playerId || multiPlayer.P2 == playerId || 
                multiPlayer.P3 == playerId || multiPlayer.P4 == playerId)
            {
                return multiPlayer; 
            }

            if (multiPlayer.P1 == null) { multiPlayer.P1 = playerId; }
            else if (multiPlayer.P2 == null) { multiPlayer.P2 = playerId; }
            else if (multiPlayer.P3 == null) { multiPlayer.P3 = playerId; }
            else if (multiPlayer.P4 == null) { multiPlayer.P4 = playerId; }
            else
                throw new InvalidOperationException("Game is full.");
            return multiPlayer;
        }
        internal async Task<Game> GetActiveGameAsync(String State, int playerId, LudoDbContext ctx, string roomCode = null)
        {
            Game game = await ctx.Games.Include(g => g.MultiPlayer).FirstOrDefaultAsync(g => g.State == State &&
                (string.IsNullOrEmpty(roomCode) || g.RoomCode == roomCode) &&
                (g.MultiPlayer.P1 == playerId ||
                 g.MultiPlayer.P2 == playerId ||
                 g.MultiPlayer.P3 == playerId ||
                 g.MultiPlayer.P4 == playerId));
            return game;
        }
    }
}
