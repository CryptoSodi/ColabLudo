using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SharedCode;
using System.Collections.Concurrent;
using PlayerDto = SharedCode.PlayerDto;

namespace SignalR.Server
{// A simple command class that holds details for a command.
    
    public class LudoHub : Hub
    {
        // Thread-safe connection mappings
        public static ConcurrentDictionary<int, string> PlayerConnections = new ConcurrentDictionary<int, string>();
        public static ConcurrentDictionary<string, int> ConnectionToPlayer = new ConcurrentDictionary<string, int>();

        private readonly IDbContextFactory<LudoDbContext> _contextFactory;
        private readonly IHubContext<LudoHub> _hubContext;
        public static CryptoHelper _crypto;
        public static DatabaseManager DM;
        private static bool _initialized = false;

        public LudoHub(IDbContextFactory<LudoDbContext> contextFactory,
                      IHubContext<LudoHub> hubContext,
                      CryptoHelper crypto)
        {
            _crypto = crypto;
            _contextFactory = contextFactory;
            _hubContext = hubContext;
            if (!_initialized)
            {
                DM = new DatabaseManager(_hubContext, _contextFactory, _crypto);
                _initialized = true;
            }
        }
        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"User connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Clean up our connection mappings
            if (ConnectionToPlayer.TryRemove(Context.ConnectionId, out var pid))
            {
                PlayerConnections.TryRemove(pid, out _);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task<StateInfo> LoadPlayerData(int playerId)
        {
            // 1) Store SignalR connection
            PlayerConnections[playerId] = Context.ConnectionId;
            ConnectionToPlayer[Context.ConnectionId] = playerId;

            using var context = _contextFactory.CreateDbContext();
            LudoServer.Models.Player P = await context.Players.FirstOrDefaultAsync(p => p.PlayerId == playerId);

            return new StateInfo
            {
                GamesPlayed = P.GamesPlayed,
                GamesWon = P.GamesWon,
                GamesLost = P.GamesLost,
                BestWin = P.BestWin,
                TotalWin = P.TotalWin,
                TotalLost = P.TotalLost,
                PhoneNumber = P.PhoneNumber,
                Score = P.Score
            };
        }
        /// <summary>
        /// Call this once after authentication or lobby-join to establish mapping.
        /// </summary>
        public async Task<DepositInfo> UserConnectedSetID(int playerId)
        {
            try
            {
                // 1) Store SignalR connection
                PlayerConnections[playerId] = Context.ConnectionId;
                ConnectionToPlayer[Context.ConnectionId] = playerId;

                // 2) Await the wallet creation/restoration
                string address = await _crypto.GetOrCreateDepositAccountAsync(playerId.ToString());
                Console.WriteLine($"Send SOL here: {address}");

                // 3) Await the balance fetch
                ulong lamports = await _crypto.GetSolBalanceAsync(address);
                double sol = lamports / 1_000_000_000.0;
                string solBalance = sol.ToString();
                Console.WriteLine($"Balance: {sol} SOL");
                //4) Return a DTO
                return new DepositInfo
                {
                    Address = address,
                    SolBalance = solBalance
                };
                
            }
            catch (Exception)
            {
                Thread.Sleep(100);
                return await UserConnectedSetID(playerId);
            }
        }
        /// <summary>
        /// Helper to fetch the current caller's player ID from the connection map.
        /// </summary>
        private int GetCurrentPlayerId()
        {
            if (ConnectionToPlayer.TryGetValue(Context.ConnectionId, out var pid))
                return pid;
            throw new HubException("Player not recognized.");
        }
        public async Task<String> SendSol(int playerID, string destination, double amountInSol)
        {
            try
            {
            // 1) Store SignalR connection
            PlayerConnections[playerID] = Context.ConnectionId;
            string sig = await _crypto.SendSolAsync(playerID.ToString(), destination, amountInSol);
            Console.WriteLine($"Sent {amountInSol} SOL — tx signature: {sig}");
                return sig;
            }
            catch (Exception)
            {
            }
            return "ERROR";
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
                seats.Add(new SharedCode.PlayerDto { PlayerId = P.PlayerId, PlayerName = P.Name, PlayerPicture = P.PictureUrl, PlayerColor = "Red" });
            }
            if (existingGame.MultiPlayer.P2 != null)
            {
                LudoServer.Models.Player P = await context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P2);
                if (existingGame.GameType == "2")
                    seats.Add(new SharedCode.PlayerDto { PlayerId = P.PlayerId, PlayerName = P.Name, PlayerPicture = P.PictureUrl, PlayerColor = "Yellow" });
                else
                    seats.Add(new SharedCode.PlayerDto { PlayerId = P.PlayerId, PlayerName = P.Name, PlayerPicture = P.PictureUrl, PlayerColor = "Green" });
            }
            if (existingGame.MultiPlayer.P3 != null)
            {
                LudoServer.Models.Player P = await context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P3);
                seats.Add(new SharedCode.PlayerDto { PlayerId = P.PlayerId, PlayerName = P.Name, PlayerPicture = P.PictureUrl, PlayerColor = "Yellow" });
            }
            if (existingGame.MultiPlayer.P4 != null)
            {
                LudoServer.Models.Player P = await context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P4);
                seats.Add(new SharedCode.PlayerDto { PlayerId = P.PlayerId, PlayerName = P.Name, PlayerPicture = P.PictureUrl, PlayerColor = "Blue" });
            }

            if (existingGame.GameType == seats.Count + "" || (seats.Count == 4 && existingGame.GameType == "22"))
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
                
                await Clients.Group(existingGame.RoomCode).SendAsync("GameStarted", existingGame.GameType, JsonConvert.SerializeObject(seats), gameRoom.engine.EngineHelper.rollsString);
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

        /* CHAT AND FRIENDS MANAGEMENT */
        public List<ChatMessages> SendChatMessage(ChatMessages CM, string roomCode)
        {
            if (roomCode != null && roomCode != "")
            {
                // Now use the user's Room property to get the GameRoom.
                if (!DM._gameRooms.TryGetValue(roomCode, out GameRoom gameRoom))
                {
                    Console.WriteLine($"GameRoom not found for room: {roomCode}");
                    return new();
                }

                if (CM.Message != "")
                    gameRoom.chatMessages.Add(CM);
                List<User> otherUsers = gameRoom.Users.Where(p => p.PlayerId != CM.SenderId).ToList();
                User senderUser = gameRoom.Users.Where(p => p.PlayerId == CM.SenderId).ToList()[0];
                CM.SenderColor = senderUser.PlayerColor;

                foreach (User u in otherUsers)
                {
                    if (CM.Message != "")
                        Clients.Client(PlayerConnections[u.PlayerId]).SendAsync("ReceiveChatHistory", CM);
                }
                return gameRoom.chatMessages.Take(20).ToList();
            }
            else 
            {
                using var context = _contextFactory.CreateDbContext();
                if(CM.Message != "")
                {
                    // 1️⃣ Save the new message to the database
                    CM.Time = DateTime.UtcNow;
                    ChatMessageEntity newMessage = new ChatMessageEntity
                    {
                        SenderId = CM.SenderId,
                        SenderName = CM.SenderName,
                        SenderColor = CM.SenderColor,
                        SenderPicture = CM.SenderPicture,
                        ReceiverId = CM.ReceiverId,
                        ReceiverName = CM.ReceiverName,
                        Message = CM.Message,
                        Time = DateTime.UtcNow  // Set the timestamp here
                    };
                    context.ChatMessages.Add(newMessage);
                    context.SaveChanges();
                }

                List<ChatMessageEntity> chatHistory = context.ChatMessages.Where(cm => 
                (cm.SenderId == CM.SenderId && cm.ReceiverId == CM.ReceiverId) ||
                (cm.SenderId == CM.ReceiverId && cm.ReceiverId == CM.SenderId)).OrderBy(cm => cm.Index).Take(30).ToList();

                // 3️⃣ Convert to the response model
                List<ChatMessages> chatMessagesList = chatHistory.Select(cm => new ChatMessages
                { 
                    Index = cm.Index,
                    SenderId = cm.SenderId,
                    SenderName = cm.SenderName,
                    SenderColor = cm.SenderColor,
                    SenderPicture = cm.SenderPicture,
                    ReceiverId = cm.ReceiverId,
                    ReceiverName = cm.ReceiverName,
                    Message = cm.Message,
                    Time = cm.Time
                }).ToList();

                //chatMessagesList.Add(CM);
                // Optionally, also send back the last 50 messages to the sender
                // send only to the receiver
                if (PlayerConnections.TryGetValue(CM.ReceiverId, out var connId) && CM.Message != "")
                {
                    Clients.Client(connId).SendAsync("ReceiveChatHistory", CM);
                }
                return chatMessagesList.Take(30).ToList();
            }
            return new List<ChatMessages>();
        }
        /* END CHAT AND FRIENDS MANAGEMENT */

        // ----------------
        // DAILY BONUS API
        // ----------------
        // Fetch or initialize the player's daily bonus record
        public async Task<DailyBonusDto> GetDailyBonus()
        {
            var playerId = GetCurrentPlayerId();
            using var ctx = _contextFactory.CreateDbContext();

            // Fetch the record (or null)
            var bonus = await ctx.DailyBonus
                                 .FirstOrDefaultAsync(x => x.PlayerId == playerId);
            var now = DateTime.UtcNow;
            var today = now.Date;
            var weekdayIndex = (int)now.DayOfWeek; // Sunday=0, Monday=1, …

            if (bonus == null)
            {
                // First‐time setup
                bonus = new DailyBonus
                {
                    PlayerId = playerId,
                    Day1 = false,
                    Day2 = false,
                    Day3 = false,
                    Day4 = false,
                    Day5 = false,
                    Day6 = false,
                    Day7 = false,
                    DayCounter = weekdayIndex,
                    LastResetDate = today.AddDays(-1)
                };
                ctx.DailyBonus.Add(bonus);
            }
            else if (bonus.LastResetDate < today && weekdayIndex == 1)
            {
                bonus.Day1 = bonus.Day2 = bonus.Day3 = bonus.Day4 =
                bonus.Day5 = bonus.Day6 = bonus.Day7 = false;

                // Reset your counter back to Monday (1)
                bonus.DayCounter = weekdayIndex;
            }
            // else: same day, nothing to reset
            try
            {
                await ctx.SaveChangesAsync();
            }
            catch (Exception ex)
            {
            }
            return new DailyBonusDto
            {
                DailyBonusId = bonus.DailyBonusId,
                PlayerId = bonus.PlayerId,
                Day1 = bonus.Day1,
                Day2 = bonus.Day2,
                Day3 = bonus.Day3,
                Day4 = bonus.Day4,
                Day5 = bonus.Day5,
                Day6 = bonus.Day6,
                Day7 = bonus.Day7,
                Bonus = 10,
                DayCounter = weekdayIndex
            };
        }
        // New function: Claim today's bonus and update LastResetDate
        public async Task<DailyBonusDto> ClaimTodayBonus()
        {
            var playerId = GetCurrentPlayerId();
            using var ctx = _contextFactory.CreateDbContext();

            var bonus = await ctx.DailyBonus.FirstOrDefaultAsync(x => x.PlayerId == playerId);
            var today = DateTime.UtcNow.Date;
            var weekdayIndex = (int)DateTime.UtcNow.DayOfWeek; // Sunday=0, Monday=1, …

            if (bonus == null)
            {
                // Initialize record if missing
                bonus = new DailyBonus
                {
                    PlayerId = playerId,
                    Day1 = false,
                    Day2 = false,
                    Day3 = false,
                    Day4 = false,
                    Day5 = false,
                    Day6 = false,
                    Day7 = false,
                    DayCounter = weekdayIndex,
                    LastResetDate = today
                };
                ctx.DailyBonus.Add(bonus);
            }


            bool alreadyClaimed = weekdayIndex switch
            {
                0 => bonus.Day1,
                1 => bonus.Day2,
                2 => bonus.Day3,
                3 => bonus.Day4,
                4 => bonus.Day5,
                5 => bonus.Day6,
                6 => bonus.Day7,
                _ => true
            };

            if (!alreadyClaimed)
            {
                // Mark today's day flag
                switch (weekdayIndex)
                {
                    case 0: bonus.Day1 = true; break;
                    case 1: bonus.Day2 = true; break;
                    case 2: bonus.Day3 = true; break;
                    case 3: bonus.Day4 = true; break;
                    case 4: bonus.Day5 = true; break;
                    case 5: bonus.Day6 = true; break;
                    case 6: bonus.Day7 = true; break;
                }

                // Update LastResetDate to today
                bonus.LastResetDate = today;
                bonus.DayCounter = weekdayIndex;

                await ctx.SaveChangesAsync();

                // Transfer bonus logic here
                int bonusAmount = 10;
                //await TransferBonusToPlayer(playerId, bonusAmount); // <- Your own logic/method
            }

            return new DailyBonusDto
            {
                DailyBonusId = bonus.DailyBonusId,
                PlayerId = bonus.PlayerId,
                Day1 = bonus.Day1,
                Day2 = bonus.Day2,
                Day3 = bonus.Day3,
                Day4 = bonus.Day4,
                Day5 = bonus.Day5,
                Day6 = bonus.Day6,
                Day7 = bonus.Day7,
                Bonus = 10,
                DayCounter = weekdayIndex
            };
        }
        /* END DAILY BONUS */
        public async Task<string> CreateJoinLobby(PlayerDto player, SharedCode.GameDto gameDTO)
        {
            Game gameRoom = await DM.JoinGameLobby(Context.ConnectionId, player, gameDTO);

            if (gameRoom == null)
            {
                return "Room is full";
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, gameRoom.RoomCode);

            await BroadcastPlayersAsync(gameRoom);

            return gameRoom.RoomCode; // Return the room name to the client
        }
        private async Task BroadcastPlayersAsync(Game existingGame)
        {
            using var context = _contextFactory.CreateDbContext();
            // Notify others in the room that a new user has joined
            if (existingGame.MultiPlayer.P1 != null)
            {
                try
                {
                    Player P1 = await context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P1);
                    await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P1", P1.PlayerId, P1.Name, P1.PictureUrl);
                }
                catch (Exception ex)
                {
                }
            }
            else
                await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P1", 0, "Waiting", "user.png");
            if (existingGame.MultiPlayer.P2 != null)
            {
                var P2 = await context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P2);
                await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P2", P2.PlayerId, P2.Name, P2.PictureUrl);
            }
            else
                await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P2", 0, "Waiting", "user.png");
            if (existingGame.MultiPlayer.P3 != null)
            {
                var P3 = await context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P3);
                await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P3", P3.PlayerId, P3.Name, P3.PictureUrl);
            }
            else
                await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P3", 0, "Waiting", "user.png");
            if (existingGame.MultiPlayer.P4 != null)
            {
                var P4 = await context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P4);
                await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", "P4", P4.PlayerId, P4.Name, P4.PictureUrl);
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