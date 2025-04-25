using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SharedCode;

namespace SignalR.Server
{// A simple command class that holds details for a command.
    
    public class LudoHub : Hub
    {
        private readonly IDbContextFactory<LudoDbContext> _contextFactory;
        private readonly IHubContext<LudoHub> _hubContext;// Better to use IHubContext if needed outside hub instances
        //public static Engine eng;// = new Engine("4", "4", "red");
        public static DatabaseManager DM;
        private static bool _initialized = false;
        public LudoHub(IDbContextFactory<LudoDbContext> contextFactory, IHubContext<LudoHub> hubContext)
        {
            _contextFactory = contextFactory;
            _hubContext = hubContext;
            if (!_initialized)
            {
                DM = new DatabaseManager(_hubContext, _contextFactory);
                _initialized = true;
            }
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {/*
            if (_users.TryGetValue(Context.ConnectionId, out var user))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, user.roomCode);
                await Clients.Group(user.roomCode).SendAsync("UserLeft", user.PlayerName);
            }*/
        }
        public override async Task OnConnectedAsync()
        {
            // Get the connection ID of the newly connected user
            var connectionId = Context.ConnectionId;
            // Print the connection message to the console
            Console.WriteLine($"User connected: {connectionId}");
            await base.OnConnectedAsync();
        }
        public Task<List<GameCommand>> PullCommands(int lastSeenIndex, String RoomCode)
        {
            if (!DM._gameRooms.TryGetValue(RoomCode, out GameRoom gameRoom))
            {
                Console.WriteLine($"GameRoom not found for room: {RoomCode}");
                return Task.FromResult(new List<GameCommand>());
            }
            
            return gameRoom.PullCommands(lastSeenIndex);
        }
        public async Task LeaveCloseLobby(int playerId, string roomCode)
        {
            if (roomCode != null)
                try
                {
                    var (existingGame, user) = await DM.LeaveGameLobby(Context.ConnectionId, playerId, roomCode);
                    // Optionally, perform additional cleanup or update the game engine state.
                    // For example: engine.RemoveUser(user); // if your engine supports this
                    await _hubContext.Clients.Group(roomCode).SendAsync("PlayerLeft", user.PlayerColor);
                    // Notify all connected clients that a user has left.
                    await BroadcastPlayersAsync(existingGame);
                }
                catch (Exception)
                {
                }
        }
        private async Task gameStartAsync(Game existingGame)
        {
            using var context = _contextFactory.CreateDbContext();
            List<SharedCode.PlayerDto> seats = new List<SharedCode.PlayerDto>();

            if (existingGame.MultiPlayer.P1 != null)
            {
                LudoServer.Models.Player P = await context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P1);
                seats.Add(new SharedCode.PlayerDto { PlayerId = P.PlayerId, PlayerName = P.PlayerName, PlayerPicture = P.PlayerPicture, PlayerColor = "Red" });
            }
            if (existingGame.MultiPlayer.P2 != null)
            {
                LudoServer.Models.Player P = await context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P2);
                if (existingGame.Type == "2")
                    seats.Add(new SharedCode.PlayerDto { PlayerId = P.PlayerId, PlayerName = P.PlayerName, PlayerPicture = P.PlayerPicture, PlayerColor = "Yellow" });
                else
                    seats.Add(new SharedCode.PlayerDto { PlayerId = P.PlayerId, PlayerName = P.PlayerName, PlayerPicture = P.PlayerPicture, PlayerColor = "Green" });
            }
            if (existingGame.MultiPlayer.P3 != null)
            {
                LudoServer.Models.Player P = await context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P3);
                seats.Add(new SharedCode.PlayerDto { PlayerId = P.PlayerId, PlayerName = P.PlayerName, PlayerPicture = P.PlayerPicture, PlayerColor = "Yellow" });
            }
            if (existingGame.MultiPlayer.P4 != null)
            {
                LudoServer.Models.Player P = await context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P4);
                seats.Add(new SharedCode.PlayerDto { PlayerId = P.PlayerId, PlayerName = P.PlayerName, PlayerPicture = P.PlayerPicture, PlayerColor = "Blue" });
            }

            if (existingGame.Type == seats.Count + "" || (seats.Count == 4 && existingGame.Type == "22"))
            {
                existingGame.State = "Playing";
                await DM.SaveData();

                await Task.Delay(2000);

                DM._gameRooms.TryGetValue(existingGame.RoomCode, out GameRoom gameRoom);
                gameRoom.seats = seats;
                gameRoom.InitializeEngine(seats[0].PlayerColor);
                for (int i = 0; i < gameRoom.Users.Count; i++)
                    gameRoom.Users[i].PlayerColor = seats[i].PlayerColor.ToLower();

                // _engine.TryAdd(existingGame.RoomCode, gameRoom);
                
                await Clients.Group(existingGame.RoomCode).SendAsync("GameStarted", existingGame.Type, JsonConvert.SerializeObject(seats), gameRoom.engine.EngineHelper.rollsString);
            }
        }
        public async Task<string> Ready(string roomCode)
        {
            Game existingGame = DM.games.FirstOrDefault(g => g.RoomCode == roomCode);
            //existingGame.MultiPlayer = await context.MultiPlayers.FirstOrDefaultAsync(m => m.MultiPlayerId == existingGame.MultiPlayerId);
            await BroadcastPlayersAsync(existingGame);
            await gameStartAsync(existingGame);
            //await BroadcastPlayersAsync(existingGame, roomCode);
            return "ready";
        }
        public GameCommand Send(string name, GameCommand commandValue, string commandtype, string roomCode)
        {
            GameCommand Result = new GameCommand();
            Console.WriteLine($"{name}: {commandValue}:{commandtype}");
            // Now use the user's Room property to get the GameRoom.
            if (!DM._gameRooms.TryGetValue(roomCode, out GameRoom gameRoom))
            {
                Console.WriteLine($"GameRoom not found for room: {roomCode}");
                //
                Result.Result = "Error: Room not found.";
                return Result;
            }
            // For logging purposes, show which room this command is coming from.
            Console.WriteLine($"{name} (room {roomCode}): {commandValue}:{commandtype}");
            // Ensure the game room's engine is initialized.
            if (gameRoom.engine == null)
            {
                Console.WriteLine($"Engine not initialized for room: {roomCode}");                
                Result.Result = "Error: Engine not initialized.";
                return Result;
            }

            // Process command based on the type.
            if (commandtype == "MovePiece")
            {
                Result = gameRoom.MovePieceAsync(commandValue).GetAwaiter().GetResult();
                return Result;
            }
            else if (commandtype == "DiceRoll")
            {
                // For other command types, for example, SeatTurn:
                // If SeatTurn returns a string, you can wait for it.
                
                Result = gameRoom.SeatTurn(commandValue).GetAwaiter().GetResult();
                return Result;
            }
            return null;
        }
        public async Task<string> CreateJoinLobby(int playerId, string userName, string pictureUrl, string gameType, decimal gameCost, string roomCode)
        {
            Game gameRoom = await DM.JoinGameLobby(Context.ConnectionId, playerId, userName, roomCode, gameType, gameCost);

            if (gameRoom == null)
            {
                return "Room is full";
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, gameRoom.RoomCode);

            BroadcastPlayersAsync(gameRoom);

            return gameRoom.RoomCode; // Return the room name to the client
        }
        private async Task BroadcastPlayersAsync(Game existingGame)
        {
            using var context = _contextFactory.CreateDbContext();
            // Notify others in the room that a new user has joined
            if (existingGame.MultiPlayer.P1 != null)
            {
                var P1 = await context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P1);
                await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P1", P1.PlayerId, P1.PlayerName, P1.PlayerPicture);
            }
            else
                await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P1", 0, "Waiting", "user.png");
            if (existingGame.MultiPlayer.P2 != null)
            {
                var P2 = await context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P2);
                await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P2", P2.PlayerId, P2.PlayerName, P2.PlayerPicture);
            }
            else
                await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P2", 0, "Waiting", "user.png");
            if (existingGame.MultiPlayer.P3 != null)
            {
                var P3 = await context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P3);
                await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P3", P3.PlayerId, P3.PlayerName, P3.PlayerPicture);
            }
            else
                await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P3", 0, "Waiting", "user.png");
            if (existingGame.MultiPlayer.P4 != null)
            {
                var P4 = await context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P4);
                await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P4", P4.PlayerId, P4.PlayerName, P4.PlayerPicture);
            }
            else
                await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P4", 0, "Waiting", "user.png");
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