﻿using LudoServer.Data;
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
            Game existingGame;
            if (gameDTO.IsPracticeGame && string.IsNullOrWhiteSpace(gameDTO.RoomCode))
            {
                existingGame = games.FirstOrDefault(g => g.BetAmount == gameDTO.BetAmount && g.PlayerCount == gameDTO.PlayerCount && g.GameType == gameDTO.GameType && g.State == "Active");//await _context.
                if (existingGame == null)
                {
                    if (string.IsNullOrWhiteSpace(gameDTO.RoomCode))
                        // Generates a unique room name
                        gameDTO.RoomCode = GenerateUniqueRoomId(gameDTO);
                    // Check if the RoomCode already exists in the database

                    existingGame = games.FirstOrDefault(g => g.RoomCode == gameDTO.RoomCode);//await _context.FirstOrDefaultAsync
                }
            }
            else
            {
                //Generate a new room name if roomName is empty
                if (string.IsNullOrWhiteSpace(gameDTO.RoomCode))
                    // Generates a unique room name
                    gameDTO.RoomCode = GenerateUniqueRoomId(gameDTO);
                // Check if the RoomCode already exists in the database
                existingGame = games.FirstOrDefault(g => g.RoomCode == gameDTO.RoomCode);//await _context.FirstOrDefaultAsync
            }

            MultiPlayer multiPlayer = await GetGamePlayers(player.PlayerId, existingGame);

            if (multiPlayer == null)
            {
                return null;//All Player Seats taken
            }
            if (existingGame == null)
            {
                multiPlayer.RoomCode = int.Parse(gameDTO.RoomCode);
                // RoomCode does not exist, create a new game entry
                existingGame = new Game
                {
                    MultiPlayerId = multiPlayer.MultiPlayerId,
                    PlayerCount = gameDTO.PlayerCount,
                    GameType= gameDTO.GameType,
                    BetAmount = gameDTO.BetAmount,
                    RoomCode = gameDTO.RoomCode,
                    Owner = player.PlayerId.ToString(),
                    State = "Active"
                };
                existingGame.MultiPlayer = multiPlayer;
                games.Add(existingGame);
                // await _context.SaveChangesAsync(); // Save the game entry to the database
            }

            // Create or retrieve the room
            GameRoom gameRoom = _gameRooms.GetOrAdd(existingGame.RoomCode, _ => new GameRoom(_hubContext, _contextFactory, _crypto,  gameDTO));

            // Add the user to the users dictionary (string ConnectionId, string Room, int PlayerId, string PlayerName, string PlayerColor)
            var user = new User(ConnectionId, existingGame.RoomCode, player.PlayerId, player.PlayerName, "Color");
            _users.GetOrAdd(ConnectionId, user);
            // Add the user to the room's user list
            gameRoom.Users.Add(user);
            // Add the user to the specified group (room)



            SaveData(); // Run save in a background thread (non-blocking)
            //Task.Run(SaveData); // Run save in a background thread (non-blocking)
            return existingGame;
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

                if (existingGame.MultiPlayer.P1 == null && existingGame.MultiPlayer.P2 == null && existingGame.MultiPlayer.P3 == null && existingGame.MultiPlayer.P4 == null)
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
            SaveData(); // Run save in a background thread (non-blocking)
            return (existingGame, user);
        }
        private string GenerateUniqueRoomId(SharedCode.GameDto gameDTO)
            //string gameType, decimal gameCost)
        {
            string roomCode;
            do
            {
                roomCode = new Random().Next(10000000, 99999999).ToString();
            }
            while (_gameRooms.ContainsKey(roomCode));

            _gameRooms.TryAdd(roomCode, new GameRoom(_hubContext, _contextFactory, _crypto, gameDTO));

            return roomCode;
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