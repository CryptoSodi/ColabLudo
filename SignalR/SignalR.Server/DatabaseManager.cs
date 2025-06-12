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
            _hubContext = hubContext;
            _contextFactory = contextFactory;
            _crypto = crypto;
            Task.Run(LoadData); // Run async without blocking constructor
        }
        public async Task<Game> JoinGameLobby(string ConnectionId, SharedCode.PlayerDto player, SharedCode.GameDto gameDTO)
        {
            Game existingGame = null;
            int? tournamentId = null;

            if (gameDTO.IsTournamentGame)
            {
                if (!int.TryParse(gameDTO.RoomCode, out int parsedId))
                    throw new ArgumentException("Invalid tournament ID format in RoomCode.");
                tournamentId = parsedId;
                existingGame = games.FirstOrDefault(g => g.TournamentId == tournamentId && g.State == "Active");
            }
            else if (gameDTO.IsPracticeGame)
                existingGame = games.FirstOrDefault(g => g.GameType == gameDTO.GameType && g.State == "Active");
            else
                existingGame = games.FirstOrDefault(g => g.RoomCode == gameDTO.RoomCode && g.State == "Active");

            if (existingGame == null)
            {
                do
                {
                    gameDTO.RoomCode = new Random().Next(10000000, 99999999).ToString();// Generates a unique room name
                    existingGame = games.FirstOrDefault(g => g.RoomCode == gameDTO.RoomCode);// Check if the RoomCode already exists in the database
                } while (existingGame != null && _gameRooms.ContainsKey(gameDTO.RoomCode));

                _gameRooms.TryAdd(gameDTO.RoomCode, new GameRoom(_hubContext, _contextFactory, _crypto, gameDTO));

                MultiPlayer multiPlayer = GetGamePlayers(player.PlayerId, null);

                multiPlayer.RoomCode = int.Parse(gameDTO.RoomCode);
                // RoomCode does not exist, create a new game entry
                existingGame = new Game
                {
                    MultiPlayerId = multiPlayer.MultiPlayerId,
                    PlayerCount = gameDTO.PlayerCount,
                    GameType = gameDTO.GameType,
                    BetAmount = gameDTO.BetAmount,
                    RoomCode = gameDTO.RoomCode,
                    IsPrivate = gameDTO.IsPrivateGame,
                    TournamentId = tournamentId,
                    Owner = player.PlayerId.ToString(),
                    State = "Active",
                    MultiPlayer = multiPlayer
                };
                games.Add(existingGame);
                //Deduct the bet amount from the player's balance if it's a paid game

                // await _context.SaveChangesAsync(); // Save the game entry to the database
            }
            else
            {
                existingGame.MultiPlayer = GetGamePlayers(player.PlayerId, existingGame);
            }

            // Create or retrieve the room
            GameRoom gameRoom = _gameRooms.GetOrAdd(existingGame.RoomCode, _ => new GameRoom(_hubContext, _contextFactory, _crypto, gameDTO));

            // Add the user to the users dictionary (string ConnectionId, string Room, int PlayerId, string PlayerName, string PlayerColor)
            var user = new User(ConnectionId, existingGame.RoomCode, player.PlayerId, player.PlayerName, "Color");
            _users.GetOrAdd(ConnectionId, user);
            // Add the user to the room's user list
            gameRoom.Users.Add(user);
            // Add the user to the specified group (room)

            await SaveData(); // Run save in a background thread (non-blocking)
            //Task.Run(SaveData); // Run save in a background thread (non-blocking)
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
        public async Task<(Game game, User user)> LeaveGameLobby(string ConnectionId, int playerId, string roomCode)
        {
            Game existingGame = games.FirstOrDefault(g => g.RoomCode == roomCode);//await _context.FirstOrDefaultAsync
            if (existingGame != null && existingGame.State == "Active")
            {
                if (existingGame?.MultiPlayer.P1 == playerId)
                    existingGame.MultiPlayer.P1 = null;
                else if (existingGame?.MultiPlayer.P2 == playerId)
                    existingGame.MultiPlayer.P2 = null;
                else if (existingGame?.MultiPlayer.P3 == playerId)
                    existingGame.MultiPlayer.P3 = null;
                else if (existingGame?.MultiPlayer.P4 == playerId)
                    existingGame.MultiPlayer.P4 = null;

                if (existingGame?.MultiPlayer.P1 == null && existingGame?.MultiPlayer.P2 == null && existingGame?.MultiPlayer.P3 == null && existingGame?.MultiPlayer.P4 == null)
                {
                    existingGame.State = "Terminated";
                }
            }

            if (_gameRooms.TryGetValue(roomCode, out GameRoom gameRoom))
            {
                gameRoom.PlayerLeft(ConnectionId, roomCode);
            }
            if (_users.TryRemove(ConnectionId, out User user))
            {
                Console.WriteLine("User not removed for connection: " + ConnectionId);
            }
            await SaveData(); // Run save in a background thread (non-blocking)
            return (existingGame, user);
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
                        IsPracticeGame = game.BetAmount==0,
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