using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SharedCode.CoreEngine;
using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace SignalR.Server
{
    public record User(int playerId, string Name, string Room);
    public record Message(string User, string Text);
    public class LudoHub : Hub
    {
        private readonly LudoDbContext _context;
        //public static Engine eng;// = new Engine("4", "4", "red");
        private static ConcurrentDictionary<string, Engine> eng = new();
        private static ConcurrentDictionary<string, User> _users = new();
        private static ConcurrentDictionary<string, GameRoom> _rooms = new();
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_users.TryGetValue(Context.ConnectionId, out var user))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, user.Room);
                await Clients.Group(user.Room).SendAsync("UserLeft", user.Name);
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

        public LudoHub(LudoDbContext context)
        {
            _context = context;
        }
        public string Send(string name, string message, string commandtype)
        {
            Console.WriteLine($"{name}: {message}:{commandtype}");
            //  Clients.All.SendAsync("addMessage", name, GameID);
            if (commandtype == "MovePiece")
            {
                //eng.MovePieceAsync(message);
            }
            else
            {
                return "";// eng.SeatTurn(message).GetAwaiter().GetResult();
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
            List<PlayerDto> seats = new List<PlayerDto>();

            if (existingGame.MultiPlayer.P1 != null)
            {
                LudoServer.Models.Player P = await _context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P1);
                seats.Add(new PlayerDto { PlayerId = P.PlayerId, PlayerName = P.PlayerName, PlayerPicture = P.PlayerPicture, PlayerColor="Red"});
            }
            if (existingGame.MultiPlayer.P2 != null)
            {
                LudoServer.Models.Player P = await _context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P2);
                if (existingGame.Type == "2")
                    seats.Add(new PlayerDto { PlayerId = P.PlayerId, PlayerName = P.PlayerName, PlayerPicture = P.PlayerPicture, PlayerColor = "Yellow" });
                else
                    seats.Add(new PlayerDto { PlayerId = P.PlayerId, PlayerName = P.PlayerName, PlayerPicture = P.PlayerPicture, PlayerColor = "Green" });
            }
            if (existingGame.MultiPlayer.P3 != null)
            {
                LudoServer.Models.Player P = await _context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P3);
                seats.Add(new PlayerDto { PlayerId = P.PlayerId, PlayerName = P.PlayerName, PlayerPicture = P.PlayerPicture, PlayerColor = "Yellow" });
            }
            if (existingGame.MultiPlayer.P4 != null)
            {
                LudoServer.Models.Player P = await _context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P4);
                seats.Add(new PlayerDto { PlayerId = P.PlayerId, PlayerName = P.PlayerName, PlayerPicture = P.PlayerPicture, PlayerColor = "Blue" });
            }

            if (existingGame.Type == seats.Count + "")//add 22
            {
                await Task.Delay(2000);

                string seatsData = JsonConvert.SerializeObject(seats);
                
                await Clients.Group(existingGame.RoomCode).SendAsync("GameStarted", existingGame.Type, seatsData);

                existingGame.State = "Playing";
                 _context.Games.Update(existingGame);
                await _context.SaveChangesAsync();

                eng.TryAdd(existingGame.RoomCode,new Engine(existingGame.Type, seats.Count + "", seats[0].PlayerColor));
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
        // Generate a unique 10-digit room ID
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
            _rooms.TryAdd(roomId, new GameRoom(roomId, gameType, gameCost));
            //"@Haris ADD this to the database games table"
            return roomId;
        }
        public async Task SendMessageToRoom(string roomName, string content)
        {
            var message = new Message(_users[Context.ConnectionId].Name, content);
            await Clients.Group(roomName).SendAsync("ReceiveMessage", message);
        }
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
    public class PlayerDto
    {
        public int PlayerId { get; set; }
        public string? PlayerName { get; set; }
        public string? PlayerPicture { get; set; }
        public string? PlayerColor { get; set; }
    }
}