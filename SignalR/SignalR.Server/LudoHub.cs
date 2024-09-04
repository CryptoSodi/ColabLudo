using LudoClient;
using LudoClient.CoreEngine;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SignalR.Server.Data;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Xml.Linq;

namespace SignalR.Server
{
    public record User(string Name, string Room);
    public record Message(string User, string Text);
    public class LudoHub : Hub
    {
        private readonly SignalRServerDbContext _context;

        public LudoHub(SignalRServerDbContext context)
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
        public async Task<string> CreateJoinRoomAsync(string userName, string gameType, int gameCost, string roomCode)
        {
            //Generate a new room name if roomName is empty
            if (string.IsNullOrWhiteSpace(roomCode))
            {
                roomCode = GenerateUniqueRoomId(gameType, gameCost); // Generates a unique room name
            }

            //@Haris - adding this to the database if it doesnot exist
            // Check if the RoomCode already exists in the database
            var existingGame = await _context.Games
                .FirstOrDefaultAsync(g => g.RoomCode == roomCode);

            if (existingGame != null)
            {
                // RoomCode exists, retrieve the existing game data
                // You can return the existing game's RoomCode or any other relevant information
                return existingGame.RoomCode;
            }
            else
            {
                // RoomCode does not exist, create a new game entry
                var game = new Game
                {
                    Type = gameType,
                    BetAmount = gameCost,
                    RoomCode = roomCode
                };

                _context.Games.Add(game);
                await _context.SaveChangesAsync();

                // Return the RoomCode of the newly created game
                return game.RoomCode;
            }

            // Create or retrieve the room
            var room = _rooms.GetOrAdd(roomCode, _ => new GameRoom(roomCode, gameType, gameCost));
            // Add the user to the users dictionary
            var user = new User(userName, roomCode);
            _users.TryAdd(Context.ConnectionId, user);
            // Add the user to the room's user list
            room.Users.Add(user);
            // Add the user to the specified group (room)
            Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
            // Notify others in the room that a new user has joined
            Clients.Group(roomCode).SendAsync("UserJoined", userName);
            return roomCode; // Return the room name to the client
        }
        // Generate a unique 10-digit room ID
        private string GenerateUniqueRoomId(string gameType, int gameCost)
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
        public async Task JoinRoom(string userName, string roomName)
        {
            _users.TryAdd(Context.ConnectionId, new User(userName, roomName));
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