using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace SignalR.Server
{
    public class DatabaseManager
    {
        List<Game> games = new List<Game>();
        List<MultiPlayer> multiPlayers = new List<MultiPlayer>();

        private readonly LudoDbContext _context;
        public ConcurrentDictionary<string, GameRoom> _rooms = new();
        private IHubCallerClients Clients;

        public DatabaseManager(IHubCallerClients clients, LudoDbContext context)
        {
            this.Clients = clients;
            _context = context;
            LoadData().Wait(); // Load all data on startup
        }

        public async Task<Game> GetGameLobby(int playerId, string roomCode, string gameType, decimal gameCost)
        {
            Game existingGame;
            if (gameCost == 0 && string.IsNullOrWhiteSpace(roomCode))
            {
                existingGame = games.FirstOrDefault(g => g.BetAmount == gameCost && g.State == "Active");//await _context.
                if (existingGame == null)
                {
                    if (string.IsNullOrWhiteSpace(roomCode))
                        // Generates a unique room name
                        roomCode = GenerateUniqueRoomId(gameType, gameCost);
                    // Check if the RoomCode already exists in the database
                    existingGame = games.FirstOrDefault(g => g.RoomCode == roomCode);//await _context.FirstOrDefaultAsync
                }
            }
            else
            {
                //Generate a new room name if roomName is empty
                if (string.IsNullOrWhiteSpace(roomCode))
                    // Generates a unique room name
                    roomCode = GenerateUniqueRoomId(gameType, gameCost);
                // Check if the RoomCode already exists in the database
                existingGame = games.FirstOrDefault(g => g.RoomCode == roomCode);//await _context.FirstOrDefaultAsync
            }

            MultiPlayer multiPlayer = await GetGamePlayers(playerId, existingGame);

            if (multiPlayer == null)
                return null;//All Player Seats taken
            if (existingGame == null)
            {
                multiPlayer.RoomCode = int.Parse(roomCode);
                // RoomCode does not exist, create a new game entry
                existingGame = new Game
                {
                    MultiPlayerId = multiPlayer.MultiPlayerId,
                    Type = gameType,
                    BetAmount = gameCost,
                    RoomCode = roomCode,
                    State = "Active"
                };
                existingGame.MultiPlayer = multiPlayer;
                games.Add(existingGame);
               // await _context.SaveChangesAsync(); // Save the game entry to the database
            }
             SaveData(); // Run save in a background thread (non-blocking)
            //Task.Run(SaveData); // Run save in a background thread (non-blocking)
            return existingGame;
        }
        private string GenerateUniqueRoomId(string gameType, decimal gameCost)
        {
            string roomId = "";
            do
            {
                // Generate a random 10-digit number
                roomId = new Random().Next(10000000, 99999999).ToString();
            }
            while (_rooms.ContainsKey(roomId)); // Ensure the ID is unique
            // Store the generated room ID with the game type in the games dictionary
            _rooms.TryAdd(roomId, new GameRoom(Clients, _context, roomId, gameType, gameCost));
            //"@Haris ADD this to the database games table"
            return roomId;
        }
        private async Task<MultiPlayer?> GetGamePlayers(int playerId, Game existingGame)
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
                MultiPlayer multiPlayer = multiPlayers.FirstOrDefault(m => m.MultiPlayerId == existingGame.MultiPlayerId);//await _context. MultiPlayers

                if (multiPlayer.P1 == null)
                    multiPlayer.P1 = playerId;
                else if (multiPlayer.P2 == null)
                    multiPlayer.P2 = playerId;
                else if (multiPlayer.P3 == null)
                    multiPlayer.P3 = playerId;
                else if (multiPlayer.P4 == null)
                    multiPlayer.P4 = playerId;
                else
                    // All player slots are full
                    return null;
                existingGame.MultiPlayer = multiPlayer;
                // Save the updated multiplayer record
                //  _context.MultiPlayers.Update(multiPlayer);
                //  await _context.SaveChangesAsync();

                return multiPlayer;
            }
        }
        private async void SaveData()
        {
            try
            {
                // Update all modified multiplayer records in the database
                foreach (var multiPlayer in multiPlayers)
                {
                    _context.MultiPlayers.Update(multiPlayer);
                }
                // Update all modified games in the database
                foreach (var game in games)
                {
                    _context.Games.Update(game);
                }
                await _context.SaveChangesAsync(); // Push all changes to the database
                Console.WriteLine("Old data saved successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving data: {ex.Message}");
            }
        }
        private async Task LoadData()
        {
            try
            {
                // Fetch all games and multiPlayers from the database
                games = await _context.Games.AsNoTracking().ToListAsync();
                multiPlayers = await _context.MultiPlayers.AsNoTracking().ToListAsync();

                // Map MultiPlayer to its respective Game
                foreach (var game in games)
                {
                    game.MultiPlayer = multiPlayers.FirstOrDefault(mp => mp.MultiPlayerId == game.MultiPlayerId);

                    // Add to _rooms for tracking active game sessions
                    _rooms.TryAdd(game.RoomCode, new GameRoom(Clients, _context, game.RoomCode, game.Type, game.BetAmount));
                }

                Console.WriteLine("New data loaded successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data: {ex.Message}");
            }
        }
    }
}