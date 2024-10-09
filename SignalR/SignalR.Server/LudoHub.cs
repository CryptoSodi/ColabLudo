using LudoClient;
using LudoClient.CoreEngine;
using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace SignalR.Server
{
    public record User(int playerId, string Name, string Room);
    public record Message(string User, string Text);
    public class LudoHub : Hub
    {
        private readonly LudoDbContext _context;

        public LudoHub(LudoDbContext context)
        {
            _context = context;
        }
        public static String GameID = "12";
        public static Engine eng = new Engine(new Gui(new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new PlayerSeat(), new PlayerSeat(), new PlayerSeat(), new PlayerSeat()));
        private static ConcurrentDictionary<string, User> _users = new();

        private static ConcurrentDictionary<string, GameRoom> _rooms = new();
        public string Send(string name, string message, string commandtype)
        {
            Console.WriteLine($"{name}: {message}:{commandtype}");
            Clients.All.SendAsync("addMessage", name, GameID);
            if (commandtype == "MovePiece")
            {
                //eng.MovePiece();
            }
            else
            {
                return eng.SeatTurn(message);
            }
            return "0";
        }
        // Example method to send a message from the server to a specific client
        public void SendServerMessage(string message)
        {
            Clients.Caller.SendAsync("ReceiveMessage", "Server", message);
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_users.TryGetValue(Context.ConnectionId, out var user))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, user.Room);
                await Clients.Group(user.Room).SendAsync("UserLeft", user.Name);
            }
        }
        public async Task<string> CreateJoinRoom(int playerId, string userName, string gameType, decimal gameCost, string roomCode)
        {
            //Generate a new room name if roomName is empty
            if (string.IsNullOrWhiteSpace(roomCode))
            {
                roomCode = GenerateUniqueRoomId(gameType, gameCost); // Generates a unique room name
            }
            // Check if the RoomCode already exists in the database
            var existingGame = await _context.Games
                .FirstOrDefaultAsync(g => g.RoomCode == roomCode);
            if (existingGame != null)
            {
                // RoomCode exists, retrieve the existing game data
                roomCode = existingGame.RoomCode;
                gameType = existingGame.Type;
                gameCost = existingGame.BetAmount;

                // RoomCode exists, retrieve the associated MultiPlayer record
                var multiPlayer = await _context.MultiPlayers
                    .FirstOrDefaultAsync(m => m.MultiPlayerId == existingGame.MultiPlayerId);

                if (multiPlayer != null)
                {
                    // Check available slots (P2, P3, P4)
                    if (multiPlayer.P2 == null)
                    {
                        multiPlayer.P2 = playerId;
                    }
                    else if (multiPlayer.P3 == null)
                    {
                        multiPlayer.P3 = playerId;
                    }
                    else if (multiPlayer.P4 == null)
                    {
                        multiPlayer.P4 = playerId;
                    }
                    else
                    {
                        // All player slots are full
                        return "All slots are full. Please join another game.";
                    }

                    // Save the updated multiplayer record
                    _context.MultiPlayers.Update(multiPlayer);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var multiPlayer = new MultiPlayer
                {
                    P1 = playerId
                };
                // Add the MultiPlayer and save changes to get the MultiPlayerId
                _context.MultiPlayers.Add(multiPlayer);
                await _context.SaveChangesAsync(); // This will save the newly added MultiPlayer and assign it an Id

                int multiPlayerId = await _context.MultiPlayers
                    .Where(g => g.P1 == playerId)
                    .Select(g => g.MultiPlayerId)
                    .FirstOrDefaultAsync();

                // RoomCode does not exist, create a new game entry
                var game = new Game
                {
                    MultiPlayerId = multiPlayerId,
                    Type = gameType,
                    BetAmount = gameCost,
                    RoomCode = roomCode
                };
                _context.Games.Add(game);
                await _context.SaveChangesAsync(); // Save the game entry to the database
            }


            // Create or retrieve the room
            var room = _rooms.GetOrAdd(roomCode, _ => new GameRoom(roomCode, gameType, gameCost));
            // Add the user to the users dictionary
            var user = new User(playerId, userName, roomCode);
            _users.TryAdd(Context.ConnectionId, user);
            // Add the user to the room's user list
            room.Users.Add(user);
            // Add the user to the specified group (room)
            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
            // Notify others in the room that a new user has joined
            await Clients.Group(roomCode).SendAsync("UserJoined", userName);
            return roomCode; // Return the room name to the client
        }
        // Generate a unique 10-digit room ID
        private string GenerateUniqueRoomId(string gameType, decimal gameCost)
        {
            string roomId;
            do
            {
                // Generate a random 10-digit number
                roomId = new Random().Next(10000000, 99999999).ToString();
            }
            while (_rooms.ContainsKey(roomId)); // Ensure the ID is unique
            // Store the generated room ID with the game type in the games dictionary
            _rooms.TryAdd(roomId, new GameRoom(roomId, gameType, gameCost));
            //"@Haris ADD this to the database games table"
            return roomId;
        }
        public async Task JoinRoom(int playerId, string userName, string roomName)
        {
            _users.TryAdd(Context.ConnectionId, new User(playerId, userName, roomName));
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
            await Clients.Group(roomName).SendAsync("UserJoined", userName);
        }
        public async Task SendMessageToRoom(string roomName, string content)
        {
            var message = new Message(_users[Context.ConnectionId].Name, content);
            await Clients.Group(roomName).SendAsync("ReceiveMessage", message);
        }
    }
}