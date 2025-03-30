using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SharedCode;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SignalR.Server
{
    public class LudoHub : Hub
    {
        private readonly LudoDbContext _context;
        //public static Engine eng;// = new Engine("4", "4", "red");
        private static ConcurrentDictionary<string, User> _users = new();
        private static ConcurrentDictionary<string, GameRoom> _engine = new();
        DatabaseManager BM;
        public LudoHub(LudoDbContext context)
        {
            _context = context;
            BM = new DatabaseManager(Clients, context);
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_users.TryGetValue(Context.ConnectionId, out var user))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, user.roomCode);
                await Clients.Group(user.roomCode).SendAsync("UserLeft", user.PlayerName);
            }
        }
        public override async Task OnConnectedAsync()
        {
            // Get the connection ID of the newly connected user
            var connectionId = Context.ConnectionId;
            // Print the connection message to the console
            Console.WriteLine($"User connected: {connectionId}");
            await base.OnConnectedAsync();
        }
        public string Send(string name, string commandValue, string commandtype)
        {
            Console.WriteLine($"{name}: {commandValue}:{commandtype}");
            //  Clients.All.SendAsync("addMessage", name, GameID);
            if (!_users.TryGetValue(Context.ConnectionId, out User user))
            {
                Console.WriteLine("User not found for connection: " + Context.ConnectionId);
                return "Error: User not found.";
            }
            // Now use the user's Room property to get the GameRoom.
            if (!_engine.TryGetValue(user.roomCode, out GameRoom gameRoom))
            {
                Console.WriteLine($"GameRoom not found for room: {user.roomCode}");
                return "Error: Room not found.";
            }
            // For logging purposes, show which room this command is coming from.
            Console.WriteLine($"{name} (room {user.roomCode}): {commandValue}:{commandtype}");
            // Ensure the game room's engine is initialized.
            if (gameRoom.engine == null)
            {
                Console.WriteLine($"Engine not initialized for room: {user.roomCode}");
                return "Error: Engine not initialized.";
            }
            // Process command based on the type.
            if (commandtype == "MovePiece")
            {
                // Asynchronously process the move (fire-and-forget or await as needed).
                // For demonstration, we're not awaiting the call.
                String piece = gameRoom.engine.MovePieceAsync(commandValue, false).GetAwaiter().GetResult();
                    SendMessageToRoom(gameRoom, Context.ConnectionId, "PieceMove", piece, "", "", false);
                return piece;
            }
            else if (commandtype == "DiceRoll")
            {
                // For other command types, for example, SeatTurn:
                // If SeatTurn returns a string, you can wait for it.
                String diveValue = gameRoom.engine.SeatTurn(commandValue, "", "", false).GetAwaiter().GetResult();
                  SendMessageToRoom(gameRoom, Context.ConnectionId, "DiceRoll", commandValue, diveValue.Split(",")[0], diveValue.Split(",")[1], false);
                //Sendtoothers(user.Room, diveValue);
                return diveValue;
            }
            return "0";
        }
        // Example method to send a message from the server to a specific client
        public void SendServerMessage(string message)
        {
            Clients.Caller.SendAsync("ReceiveMessage", "Server", message);
        }
        public async Task LeaveCloseLobby(int playerId, string roomCode)
        {
            if(roomCode!=null)
            try
            {
                // Check if the RoomCode already exists in the database
                Game existingGame = await _context.Games.FirstOrDefaultAsync(g => g.RoomCode == roomCode);
                MultiPlayer multiPlayer = existingGame.MultiPlayer = await _context.MultiPlayers.FirstOrDefaultAsync(m => m.MultiPlayerId == existingGame.MultiPlayerId);

                if (multiPlayer.P1 == playerId)
                    multiPlayer.P1 = null;
                else if (multiPlayer.P2 == playerId)
                    multiPlayer.P2 = null;
                else if (multiPlayer.P3 == playerId)
                    multiPlayer.P3 = null;
                else if (multiPlayer.P4 == playerId)
                    multiPlayer.P4 = null;

                _context.MultiPlayers.Update(multiPlayer);
                    if (!_engine.TryGetValue(roomCode, out GameRoom gameRoom))
                    {
                        Console.WriteLine($"GameRoom not found for room: {roomCode}");
                    }
                    if (!_users.TryGetValue(Context.ConnectionId, out User user))
                    {
                        Console.WriteLine("User not found for connection: " + Context.ConnectionId);
                    }
                    if (gameRoom != null)
                        gameRoom.PlayerLeft(Context.ConnectionId, roomCode);
                if (multiPlayer.P1 == null && multiPlayer.P2 == null && multiPlayer.P3 == null && multiPlayer.P4 == null)
                {
                    existingGame.State = "Terminated";
                    _context.Games.Update(existingGame);
                }
                await _context.SaveChangesAsync();
                BroadcastPlayersAsync(existingGame);
            } 
            catch (Exception)
            {
            }
        }
       
        private async Task gameStartAsync(Game existingGame)
        {
            List<SharedCode.PlayerDto> seats = new List<SharedCode.PlayerDto>();

            if (existingGame.MultiPlayer.P1 != null)
            {
                LudoServer.Models.Player P = await _context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P1);
                seats.Add(new SharedCode.PlayerDto { PlayerId = P.PlayerId, PlayerName = P.PlayerName, PlayerPicture = P.PlayerPicture, PlayerColor="Red"});
            }
            if (existingGame.MultiPlayer.P2 != null)
            {
                LudoServer.Models.Player P = await _context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P2);
                if (existingGame.Type == "2")
                    seats.Add(new SharedCode.PlayerDto { PlayerId = P.PlayerId, PlayerName = P.PlayerName, PlayerPicture = P.PlayerPicture, PlayerColor = "Yellow" });
                else
                    seats.Add(new SharedCode.PlayerDto { PlayerId = P.PlayerId, PlayerName = P.PlayerName, PlayerPicture = P.PlayerPicture, PlayerColor = "Green" });
            }
            if (existingGame.MultiPlayer.P3 != null)
            {
                LudoServer.Models.Player P = await _context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P3);
                seats.Add(new SharedCode.PlayerDto { PlayerId = P.PlayerId, PlayerName = P.PlayerName, PlayerPicture = P.PlayerPicture, PlayerColor = "Yellow" });
            }
            if (existingGame.MultiPlayer.P4 != null)
            {
                LudoServer.Models.Player P = await _context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P4);
                seats.Add(new SharedCode.PlayerDto { PlayerId = P.PlayerId, PlayerName = P.PlayerName, PlayerPicture = P.PlayerPicture, PlayerColor = "Blue" });
            }

            if (existingGame.Type == seats.Count + "" || (seats.Count == 4 && existingGame.Type == "22"))
            {
                existingGame.State = "Playing";
                _context.Games.Update(existingGame);
                await _context.SaveChangesAsync();

                await Task.Delay(2000);

                BM._rooms.TryGetValue(existingGame.RoomCode, out GameRoom gameRoom);
                gameRoom.seats = seats;
                gameRoom.InitializeEngine(seats[0].PlayerColor);
                for (int i = 0; i < gameRoom.Users.Count; i++)
                    gameRoom.Users[i].PlayerColor = seats[i].PlayerColor.ToLower();
                _engine.TryAdd(existingGame.RoomCode, gameRoom);

                await Clients.Group(existingGame.RoomCode).SendAsync("GameStarted", existingGame.Type, JsonConvert.SerializeObject(seats), gameRoom.engine.EngineHelper.rollsString);
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
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start(); // Start timing
            Game existingGame = await BM.GetGameLobby(playerId, roomCode, gameType, gameCost);

            if (existingGame == null)
            {
                return "Room is full";
            }

            roomCode = existingGame.RoomCode;
            gameType = existingGame.Type;

            // Create or retrieve the room
            var room = BM._rooms.GetOrAdd(roomCode, _ => new GameRoom(Clients, _context, roomCode, gameType, gameCost));
            // Add the user to the users dictionary (string ConnectionId, string Room, int PlayerId, string PlayerName, string PlayerColor)
            var user = new User(Context.ConnectionId, roomCode, playerId, userName, "Color");
            _users.TryAdd(Context.ConnectionId, user);
            // Add the user to the room's user list
            room.Users.Add(user);
            // Add the user to the specified group (room)
            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);

            stopwatch.Stop(); // Stop timing
            Console.WriteLine($"Execution Time: {stopwatch.ElapsedMilliseconds} ms");
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
            }else
                await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P1", 0, "Waiting", "user.png");
            if (existingGame.MultiPlayer.P2 != null)
            {
                var P2 = await _context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P2);
                await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P2", P2.PlayerId, P2.PlayerName, P2.PlayerPicture);
            }
            else
                await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P2", 0, "Waiting", "user.png");
            if (existingGame.MultiPlayer.P3 != null)
            {
                var P3 = await _context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P3);
                await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P3", P3.PlayerId, P3.PlayerName, P3.PlayerPicture);
            }
            else
                await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P3", 0, "Waiting", "user.png");
            if (existingGame.MultiPlayer.P4 != null)
            {
                var P4 = await _context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P4);
                await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P4", P4.PlayerId, P4.PlayerName, P4.PlayerPicture);
            }
            else
                await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P4", 0, "Waiting", "user.png");
        }
        public async Task SendMessageToRoom(GameRoom gameRoom, string connectionId, string SendToClientFunctionName, string SeatColor, string messageType, string value, bool sendToSelf = true)
        {
            var recipientConnectionIds = gameRoom.Users
                .Where(u => sendToSelf || u.ConnectionId != connectionId)
                .Select(u => u.ConnectionId)
                .ToList();
            foreach(String ConnectionId in recipientConnectionIds)
                if(SendToClientFunctionName == "DiceRoll")
                    await Clients.Client(ConnectionId).SendAsync(SendToClientFunctionName, SeatColor, messageType, value);
                else
                    await Clients.Client(ConnectionId).SendAsync(SendToClientFunctionName, SeatColor);
        }
        // Generate a unique 10-digit room ID
       
        //Logic for 4 players game in tournament road to final
        static void RunTournament(List<string> players)
        {
            int roundNumber = 1;

            while (players.Count > 4)
            {
                Console.WriteLine($"\nRound {roundNumber}: {players.Count} players remaining");

                // Handle cases where player count is not divisible by 4
                if (players.Count % 4 != 0)
                {
                    HandleUnevenPlayers(players);
                }

                // Divide players into groups of 4 and play matches
                List<string> winners = new List<string>();
                for (int i = 0; i < players.Count; i += 4)
                {
                    var group = players.Skip(i).Take(4).ToList();
                    string winner = PlayMatch(group);
                    winners.Add(winner);
                }

                players = winners;
                roundNumber++;
            }

            // Final round with the last 4 players
            Console.WriteLine("\nFinal Round:");
            string tournamentWinner = PlayMatch(players);
            Console.WriteLine($"\nThe tournament winner is {tournamentWinner}!");
        }
        static void HandleUnevenPlayers(List<string> players)
        {
            int remainder = players.Count % 4;

            if (remainder == 1)
            {
                // 1 player left, play against a bot
                string lonePlayer = players.Last();
                players.RemoveAt(players.Count - 1);
                Console.WriteLine($"{lonePlayer} plays against a bot.");
                string winner = PlayMatch(new List<string> { lonePlayer, "Bot" });
                players.Add(winner);
            }
            else if (remainder == 2 || remainder == 3)
            {
                // 2 or 3 players left, play a match among them
                var group = players.Skip(players.Count - remainder).ToList();
                players.RemoveRange(players.Count - remainder, remainder);
                Console.WriteLine($"{string.Join(", ", group)} play a match.");
                string winner = PlayMatch(group);
                players.Add(winner);
            }
        }
        static string PlayMatch(List<string> players)
        {
            Console.WriteLine($"Match: {string.Join(" vs ", players)}");
            Random rand = new Random();
            string winner = players[rand.Next(players.Count)];
            Console.WriteLine($"Winner: {winner}");
            return winner;
        }
    }
   
    public class User
    {
        public User(string connectionId, string roomCode, int playerId, string userName, string playerColor)
        {
            ConnectionId = connectionId;
            this.roomCode = roomCode;
            PlayerId = playerId;
            this.PlayerName = userName;
            this.PlayerColor = playerColor;
        }

        public string ConnectionId { get; init; }
        public string roomCode { get; init; }
        public int PlayerId { get; init; }
        public string PlayerName { get; init; }
        public string PlayerColor { get; set; }  // Now mutable
    }
}