using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SignalR.Server.Payments;
using SignalR.Server.Services;
using System.Collections.Concurrent;

namespace SignalR.Server
{
    public class DatabaseManager(IHubContext<LudoHub> _hubContext, IHubContext<DashboardHub> _dashboardHub, IDbContextFactory<LudoDbContext> _contextFactory, CryptoHelper _crypto, UtilService _utilService)
    {
        public ConcurrentDictionary<string, GameRoom> _gameRooms { get; set; } = new();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _roomLocks = new();

        private async Task BroadcastMatchUpdate()
        {
            await _dashboardHub.Clients.All.SendAsync("RefreshMatchCenter");
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

            // Add to GameRoom memory list
            GameRoom gameRoom = _gameRooms.GetOrAdd(existingGame.RoomCode, _ => new GameRoom(_hubContext, _contextFactory, this, _crypto, _utilService, gameDTO));
            
            lock (gameRoom.Users) 
            {
                gameRoom.Users.Add(new User(player, "Color"));
            }

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
        internal async Task<(Game existingGame, List<SharedCode.PlayerDto> seats, string rollsString)> Ready(int playerId)
        {
            using var ctx = _contextFactory.CreateDbContext();

            Game existingGame = await GetActiveGameAsync("Active", playerId, ctx);
            if (existingGame == null)
                return (null, null, null);

            var roomLock = GetRoomLock(existingGame.RoomCode);
            await roomLock.WaitAsync();

            try
            {
                // Reload inside lock
                existingGame = await GetActiveGameAsync("Active", playerId, ctx);
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
        internal async Task<Game> GetActiveGameAsync(String State, int playerId, LudoDbContext ctx)
        {
            Game game = await ctx.Games.Include(g => g.MultiPlayer).FirstOrDefaultAsync(g => g.State == State &&
                (g.MultiPlayer.P1 == playerId ||
                 g.MultiPlayer.P2 == playerId ||
                 g.MultiPlayer.P3 == playerId ||
                 g.MultiPlayer.P4 == playerId));
            return game;
        }
    }
}