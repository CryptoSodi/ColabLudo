using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace SignalR.Server
{
    public class DatabaseManager
    {
        public List<Game> games = new List<Game>();
        public ConcurrentDictionary<string, GameRoom> _gameRooms = new();

        List<MultiPlayer> multiPlayers = new List<MultiPlayer>();
        public ConcurrentDictionary<string, User> _users = new();

        private readonly IDbContextFactory<LudoDbContext> _contextFactory;
        private readonly IHubContext<LudoHub> _hubContext;
        private readonly CryptoHelper _crypto;

        public DatabaseManager(IHubContext<LudoHub> hubContext, IDbContextFactory<LudoDbContext> contextFactory, CryptoHelper crypto)
        {
            _crypto = crypto;
            _hubContext = hubContext;
            _contextFactory = contextFactory;
            Task.Run(LoadData); // Run async without blocking constructor
        }
        public async Task<Game> JoinGameLobby(string connectionId, Player player, SharedCode.GameDto gameDTO)
        {
            Console.WriteLine(DateTime.UtcNow);
            Game existingGame = null;
            int tournamentId = -1;
            using var ctx = _contextFactory.CreateDbContext();

            if (gameDTO.IsTournamentGame)
            {
                if (!int.TryParse(gameDTO.RoomCode, out int parsedId))
                    throw new ArgumentException("Invalid tournament ID format in RoomCode.");
                tournamentId = parsedId;
                var tournament = await ctx.Tournaments.FirstOrDefaultAsync(tc => tc.TournamentId == tournamentId);
                if (tournament == null)
                    throw new Exception($"Tournament {tournamentId} not found.");

                gameDTO.BetAmount = tournament.EntryFee;
                gameDTO.RoomCode = "";
                existingGame = games.FirstOrDefault(g => g.TournamentId == tournamentId && g.State == "Active");
            }
            else if (gameDTO.IsPracticeGame)
            {
                existingGame = games.FirstOrDefault(g => g.GameType == gameDTO.GameType && g.BetAmount == 0 && g.State == "Active");
            }
            else
            {
                existingGame = games.FirstOrDefault(g => g.RoomCode == gameDTO.RoomCode && g.State == "Active");
            }

            if (existingGame == null)
            {
                // Create a new game
                do
                {
                    gameDTO.RoomCode = new Random().Next(10000000, 99999999).ToString(); // Generate unique room code
                    existingGame = games.FirstOrDefault(g => g.RoomCode == gameDTO.RoomCode);
                } while (existingGame != null || _gameRooms.ContainsKey(gameDTO.RoomCode));

                // Deduct game fee
                if (!gameDTO.IsPracticeGame)
                {
                    if (!await _crypto.deductGameFee(player.PlayerId, tournamentId, gameDTO.RoomCode, gameDTO.IsTournamentGame, gameDTO.BetAmount))
                    {
                        Console.WriteLine($"Game fee FAILED TO deduct for player {player.PlayerId} in room {gameDTO.RoomCode}.");
                        return null;
                    }
                }

                _gameRooms.TryAdd(gameDTO.RoomCode, new GameRoom(_hubContext, _contextFactory, _crypto, gameDTO));

                MultiPlayer multiPlayer = GetGamePlayers(player.PlayerId, null);
                multiPlayer.RoomCode = int.Parse(gameDTO.RoomCode);

                existingGame = new Game
                {
                    MultiPlayerId = multiPlayer.MultiPlayerId,
                    PlayerCount = gameDTO.PlayerCount,
                    GameType = gameDTO.GameType,
                    BetAmount = gameDTO.BetAmount,
                    RoomCode = gameDTO.RoomCode,
                    IsPrivate = gameDTO.IsPrivateGame,
                    IsPractice = gameDTO.IsPracticeGame,
                    TournamentId = gameDTO.IsTournamentGame ? tournamentId : null,
                    Owner = player.PlayerId,
                    State = "Active",
                    MultiPlayer = multiPlayer
                };

                lock (games) // Lock the games list while modifying
                {
                    games.Add(existingGame);
                }
            }
            else
            {
                // Do async work *after* lock released
                if (!existingGame.IsPractice)
                {
                    if (!await _crypto.deductGameFee(player.PlayerId, existingGame.TournamentId, existingGame.RoomCode, gameDTO.IsTournamentGame, existingGame.BetAmount))
                    {
                        Console.WriteLine($"Game fee FAILED TO deduct for player {player.PlayerId} in room {gameDTO.RoomCode}.");
                        return null;
                    }
                }
                // Join existing game
                lock (existingGame)
                {
                    // Safely assign player slots here
                    existingGame.MultiPlayer = GetGamePlayers(player.PlayerId, existingGame);
                }
            }

            // Add to GameRoom
            GameRoom gameRoom = _gameRooms.GetOrAdd(existingGame.RoomCode, _ => new GameRoom(_hubContext, _contextFactory, _crypto, gameDTO));

            // Add user to active users
            var user = new User(connectionId, existingGame.RoomCode, player.PlayerId, player.Name, "Color");

            _users.GetOrAdd(connectionId, user);

            lock (gameRoom.Users) // Lock GameRoom users list
            {
                gameRoom.Users.Add(user);
            }

            await SaveData(); // Save asynchronously
            return existingGame;
        }
        private MultiPlayer GetGamePlayers(int playerId, Game existingGame)
        {
            if (existingGame == null)
            {
                MultiPlayer multiPlayer = new MultiPlayer
                {
                    P1 = playerId
                };
                // Add the MultiPlayer and save changes to get the MultiPlayerId
                multiPlayers.Add(multiPlayer);//_context.MultiPlayers
                //await _context.SaveChangesAsync(); // This will save the newly added MultiPlayer and assign it an Id
                
                return multiPlayer;
            }
            else
            {
                if (existingGame.MultiPlayer.P1 == null) existingGame.MultiPlayer.P1 = playerId;
                else if (existingGame.MultiPlayer.P2 == null) existingGame.MultiPlayer.P2 = playerId;
                else if (existingGame.MultiPlayer.P3 == null) existingGame.MultiPlayer.P3 = playerId;
                else if (existingGame.MultiPlayer.P4 == null) existingGame.MultiPlayer.P4 = playerId;
                else return null;
                
                return existingGame.MultiPlayer;
            }
        }
        public async Task<(Game game, User user)> LeaveGameLobby(string connectionId, int playerId)
        {
            // Find the game where this player exists
            Game existingGame = games.FirstOrDefault(g => (g.State == "Active") &&
                (g.MultiPlayer.P1 == playerId ||
                 g.MultiPlayer.P2 == playerId ||
                 g.MultiPlayer.P3 == playerId ||
                 g.MultiPlayer.P4 == playerId));
            if (existingGame != null && existingGame.State == "Active")
            {
                lock (existingGame) // Lock to prevent race conditions
                {
                    // Clear the player slot
                    if (existingGame.MultiPlayer.P1 == playerId)
                        existingGame.MultiPlayer.P1 = null;
                    else if (existingGame.MultiPlayer.P2 == playerId)
                        existingGame.MultiPlayer.P2 = null;
                    else if (existingGame.MultiPlayer.P3 == playerId)
                        existingGame.MultiPlayer.P3 = null;
                    else if (existingGame.MultiPlayer.P4 == playerId)
                        existingGame.MultiPlayer.P4 = null;

                    // Check if all player slots are empty
                    bool allPlayersLeft = existingGame.MultiPlayer.P1 == null
                        && existingGame.MultiPlayer.P2 == null
                        && existingGame.MultiPlayer.P3 == null
                        && existingGame.MultiPlayer.P4 == null;

                    if (allPlayersLeft)
                    {
                        existingGame.State = "Terminated";

                        // Remove GameRoom from memory
                        if (_gameRooms.TryRemove(existingGame.RoomCode, out var removedRoom))
                        {
                            Console.WriteLine($"GameRoom {existingGame.RoomCode} removed from memory.");
                        }
                    }
                }
                // Refund the player if this is not a practice or tournament game
                if (!existingGame.IsPractice && existingGame.TournamentId == null)
                {
                    try
                    {
                        decimal betAmount = existingGame.BetAmount;
                        await _crypto.OffChainTransaction(playerId, betAmount, "Game Refund", "", false, existingGame.RoomCode);
                        Console.WriteLine($"Refunded {betAmount} to player {playerId} for game {existingGame.RoomCode}.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Refund failed for player {playerId} in game {existingGame.RoomCode}: {ex.Message}");
                        // Optionally: Re-add the player to the game or log for manual investigation
                    }
                }
            }

            if(existingGame == null)
                existingGame = games.FirstOrDefault(g => (g.State == "Playing") &&
                (g.MultiPlayer.P1 == playerId ||
                 g.MultiPlayer.P2 == playerId ||
                 g.MultiPlayer.P3 == playerId ||
                 g.MultiPlayer.P4 == playerId));
            if (existingGame != null)
            {
                // Notify GameRoom about the player leaving
                if (_gameRooms.TryGetValue(existingGame.RoomCode, out GameRoom gameRoom))
                {
                    await gameRoom.PlayerLeft(playerId, existingGame.RoomCode);
                }

                // Remove the user from the active users list
                if (_users.TryRemove(connectionId, out User user))
                {
                    Console.WriteLine($"User {user.PlayerId} removed from active users.");
                }
                else
                {
                    Console.WriteLine($"Failed to remove user for connection: {connectionId}");
                }
            }
            // Persist the updated state
            try
            {
                await SaveData();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while saving game state: {ex.Message}");
            }
            return (existingGame, _users.ContainsKey(connectionId) ? _users[connectionId] : null);
        }
        public async Task SaveData()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();

                foreach (var multiPlayer in multiPlayers)
                {
                    context.MultiPlayers.Update(multiPlayer);
                }

                foreach (var game in games)
                {
                    context.Games.Update(game);
                }

                await context.SaveChangesAsync();
                Console.WriteLine("Data saved successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving data: {ex.Message}");
            }
        }
        public async Task LoadData()
        {
            try
            {
                using var context = _contextFactory.CreateDbContext();
                games = await context.Games.ToListAsync();
                multiPlayers = await context.MultiPlayers.ToListAsync();

                foreach (var game in games)
                {
                    game.MultiPlayer = multiPlayers.FirstOrDefault(mp => mp.MultiPlayerId == game.MultiPlayerId);
                    SharedCode.GameDto gameDto = new SharedCode.GameDto
                    {
                        GameType = game.GameType,
                        RoomCode = game.RoomCode,
                        BetAmount = game.BetAmount,
                        PlayerCount = game.PlayerCount,
                        IsPracticeGame = game.BetAmount == 0,
                        IsTournamentGame = game.TournamentId != null,
                        playerColor = "DefaultColor" // Set a default color or retrieve from the database if needed
                    };
                    _gameRooms.TryAdd(game.RoomCode, new GameRoom(_hubContext, _contextFactory, _crypto, gameDto));
                }

                Console.WriteLine("Data loaded successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data: {ex.Message}");
            }
        }
    }
}