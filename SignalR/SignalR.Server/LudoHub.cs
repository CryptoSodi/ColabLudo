using LudoClient;
using LudoClient.CoreEngine;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Xml.Linq;

namespace SignalR.Server
{
    public record User(string Name, string Room);
    public record Message(string User, string Text);
    public class LudoHub : Hub
    {
        public static String GameID = "12";
        public static Engine eng = new Engine(new Gui(new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new PlayerSeat(), new PlayerSeat(), new PlayerSeat(), new PlayerSeat()));
        private static ConcurrentDictionary<string, User> _users = new();
        private static ConcurrentDictionary<string, Games> games = new();
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
        public string CreateJoinRoom(string userName, string roomName)
        {
            // Generate a new room name if roomName is empty
            if (string.IsNullOrWhiteSpace(roomName))
            {
                roomName = GenerateUniqueRoomId(); // Generates a unique room name
            }
            // Add the user to the users dictionary
            _users.TryAdd(Context.ConnectionId, new User(userName, roomName));
            // Add the user to the specified group (room)
            Groups.AddToGroupAsync(Context.ConnectionId, roomName);
            // Notify others in the room that a new user has joined
            Clients.Group(roomName).SendAsync("UserJoined", userName);
            return roomName; // Return the room name to the client
        }
        // Generate a unique 10-digit room ID
        private string GenerateUniqueRoomId()
        {
            string roomId;
            do
            {
                                // Generate a random 10000000-digit number
                roomId = new Random().Next(10000000, 99999999).ToString();
            }
            while (games.ContainsKey(roomId)); // Ensure the ID is unique
            // Store the generated room ID in the games dictionary to avoid repetition
            games.TryAdd(roomId, new Games());
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