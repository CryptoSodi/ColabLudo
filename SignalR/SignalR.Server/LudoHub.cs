using LudoClient;
using LudoClient.CoreEngine;
using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Security.AccessControl;

namespace SignalR.Server
{
    public record User(int playerId, string Name, string Room);
    public record Message(string User, string Text);
    public class LudoHub : Hub
    {
        private readonly LudoDbContext _context;
        public static Engine eng = new Engine("4", "4", "red", new Gui(new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new PlayerSeat(), new PlayerSeat(), new PlayerSeat(), new PlayerSeat()));
        private static ConcurrentDictionary<string, User> _users = new();
        private static ConcurrentDictionary<string, GameRoom> _rooms = new();
        public LudoHub(LudoDbContext context)
        {
            _context = context;
        }
        public override async Task OnConnectedAsync()
        {
            // Get the connection ID of the newly connected user
            var connectionId = Context.ConnectionId;
            // Print the connection message to the console
            Console.WriteLine($"User connected: {connectionId}");
            await base.OnConnectedAsync();
        }
        public string Send(string name, string message, string commandtype)
        {
            Console.WriteLine($"{name}: {message}:{commandtype}");
            //  Clients.All.SendAsync("addMessage", name, GameID);
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
        public async Task<Game> GetGameLobby(int playerId, string roomCode, string gameType, decimal gameCost)
        {
            //Generate a new room name if roomName is empty
            if (string.IsNullOrWhiteSpace(roomCode))
                // Generates a unique room name
                roomCode = GenerateUniqueRoomId(gameType, gameCost);

            // Check if the RoomCode already exists in the database
            var existingGame = await _context.Games.FirstOrDefaultAsync(g => g.RoomCode == roomCode);

            MultiPlayer multiPlayer = await GetGamePlayers(playerId, existingGame);
           
            if (multiPlayer == null)
                return null;//All Player Seats taken
            if (existingGame == null)
            {
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
                _context.Games.Add(existingGame);
                await _context.SaveChangesAsync(); // Save the game entry to the database
            }
            return existingGame;
        }
        private async Task<MultiPlayer?> GetGamePlayers(int playerId, Game existingGame)
        {
            MultiPlayer multiPlayer;
            if (existingGame == null)
            {
                multiPlayer = new MultiPlayer
                {
                    P1 = playerId
                };
                // Add the MultiPlayer and save changes to get the MultiPlayerId
                _context.MultiPlayers.Add(multiPlayer);
                await _context.SaveChangesAsync(); // This will save the newly added MultiPlayer and assign it an Id
                return multiPlayer;
            }
            else
            {
                existingGame.MultiPlayer = multiPlayer = await _context.MultiPlayers.FirstOrDefaultAsync(m => m.MultiPlayerId == existingGame.MultiPlayerId);
                
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
                // Save the updated multiplayer record
                _context.MultiPlayers.Update(multiPlayer);
                await _context.SaveChangesAsync();
            }
            return multiPlayer;
        }
        private async Task gameStartAsync(Game existingGame)
        {
            int playerCounter = 0;
            if (existingGame.MultiPlayer.P1 != null)
                playerCounter++;
            if (existingGame.MultiPlayer.P2 != null)
                playerCounter++;
            if (existingGame.MultiPlayer.P3 != null)
                playerCounter++;
            if (existingGame.MultiPlayer.P4 != null)
                playerCounter++;

            if (existingGame.Type == playerCounter + "")
            {
                await Task.Delay(5000);
                Clients.Group(existingGame.RoomCode).SendAsync("GameStart");
                existingGame.State = "Playing";
                 _context.Games.Update(existingGame);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<string> Ready(string roomCode)
        {
            var existingGame = await _context.Games.FirstOrDefaultAsync(g => g.RoomCode == roomCode);
            existingGame.MultiPlayer = await _context.MultiPlayers.FirstOrDefaultAsync(m => m.MultiPlayerId == existingGame.MultiPlayerId);
            await BroadcastPlayersAsync(existingGame);
            await gameStartAsync(existingGame);
            //await BroadcastPlayersAsync(existingGame, roomCode);
            return "ready";
        }
        public async Task<string> CreateJoinLobby(int playerId, string userName, string pictureUrl, string gameType, decimal gameCost, string roomCode)
        {
            var existingGame = await GetGameLobby(playerId, roomCode, gameType, gameCost);

            if (existingGame == null)
            {
                return "Room is full";
            }

            roomCode = existingGame.RoomCode;
            gameType = existingGame.Type;
            gameCost = existingGame.BetAmount;

            // Create or retrieve the room
            var room = _rooms.GetOrAdd(roomCode, _ => new GameRoom(roomCode, gameType, gameCost));
            // Add the user to the users dictionary
            var user = new User(playerId, userName, roomCode);
            _users.TryAdd(Context.ConnectionId, user);
            // Add the user to the room's user list
            room.Users.Add(user);
            // Add the user to the specified group (room)
            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);

            BroadcastPlayersAsync(existingGame);
            return roomCode; // Return the room name to the client
        }
        private async Task BroadcastPlayersAsync(Game existingGame)
        {
            // Notify others in the room that a new user has joined
            if (existingGame.MultiPlayer.P1 != null)
            {
                var P1 = await _context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P1);
                await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P1", P1.PlayerId, P1.PlayerName, P1.PlayerPicture);
            }
            if (existingGame.MultiPlayer.P2 != null)
            {
                var P2 = await _context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P2);
                await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P2", P2.PlayerId, P2.PlayerName, P2.PlayerPicture);
            }
            if (existingGame.MultiPlayer.P3 != null)
            {
                var P3 = await _context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P3);
                await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P3", P3.PlayerId, P3.PlayerName, P3.PlayerPicture);
            }
            if (existingGame.MultiPlayer.P4 != null)
            {
                var P4 = await _context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P4);
                await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P4", P4.PlayerId, P4.PlayerName, P4.PlayerPicture);
            }
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
        public async Task SendMessageToRoom(string roomName, string content)
        {
            var message = new Message(_users[Context.ConnectionId].Name, content);
            await Clients.Group(roomName).SendAsync("ReceiveMessage", message);
        }
    }
}