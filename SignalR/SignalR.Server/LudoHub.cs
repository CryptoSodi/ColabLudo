using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Org.BouncyCastle.Cms;
using SharedCode;
using System.Collections.Concurrent;
using System.Threading.Tasks;
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
                _initialized = true;
                DM = new DatabaseManager(_hubContext, _contextFactory, _crypto);
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
                string address = await _crypto.GetOrCreateSubAccountAsync(playerId.ToString());
                Console.WriteLine($"Send SOL here: {address}");

                // 3) Await the balance fetch
                var totalBalance = await _crypto.GetTotalBalanceAsync(playerId.ToString());
                Console.WriteLine($"Balance: {totalBalance} SOL");
                //4) Return a DTO
                return new DepositInfo
                {
                    Address = address,
                    SolBalance = totalBalance.ToString()
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
        public async Task<String> SendSol(string destination, double amountInSol)
        {
            int playerId = GetCurrentPlayerId();

            try
            {
                // 0) Check total balance (on-chain + off-chain)
                var totalBalance = await _crypto.GetTotalBalanceAsync(playerId.ToString());
                if (totalBalance < (decimal)amountInSol)
                {
                    Console.WriteLine($"Withdrawal failed: insufficient total balance for {playerId}. Have {totalBalance} SOL, tried {amountInSol} SOL.");
                    return "INSUFFICIENT_FUNDS";
                }

                // 1) Debit from off-chain ledger (credit master balance)
                var debited = await _crypto.DebitToMasterOffChainAsync(playerId.ToString(), (decimal)amountInSol);
                if (!debited)
                {
                    Console.WriteLine($"Withdrawal failed: insufficient off-chain funds for {playerId}");
                    return "INSUFFICIENT_OFFCHAIN";
                }

                // 2) Send on-chain using master wallet
                var txSignature = await _crypto.SendFromMasterAsync(destination, (decimal)amountInSol);
                Console.WriteLine($"Withdrawal of {amountInSol} SOL for {playerId} sent from master. Tx: {txSignature}");
                return txSignature;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during withdrawal for {playerId}: {ex.Message}");
                return "ERROR";
            }
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
        /* TOURNAMENT API */
        public List<TournamentDTO> GetAllTournaments(string type)
        {
            using var ctx = _contextFactory.CreateDbContext();
            var playerId = GetCurrentPlayerId();
            var nowUtc = DateTime.UtcNow;

            // 1) Begin queryable for efficiency
            IQueryable<Tournament> query = ctx.Tournaments.AsNoTracking();

            // 2) Apply tournament type filter
            if (!string.IsNullOrWhiteSpace(type))
            {
                query = type switch
                {
                    "Completed" => query.Where(t => nowUtc > t.EndDate),
                    "Running" => query.Where(t => nowUtc >= t.StartDate && nowUtc <= t.EndDate),
                    "Upcoming" => query.Where(t => nowUtc < t.StartDate),
                    _ => query // Return all if type is unknown
                };
            }

            var tournaments = query.ToList();

            if (tournaments.Count == 0)
                return new List<TournamentDTO>();

            // 3) Fetch all tournament IDs this player has joined
            var joinedIds = ctx.TournamentChallengers
                .Where(tc => tc.PlayerId == playerId)
                .Select(tc => tc.TournamentId)
                .ToHashSet();

            // 4) Build DTOs
            var result = tournaments.Select(t => new TournamentDTO
            {
                TournamentId = t.TournamentId,
                Name = t.Name,
                Winner1 = t.Winner1,
                Winner2 = t.Winner2,
                Winner3 = t.Winner3,
                Prize1 = t.Prize1,
                Prize2 = t.Prize2,
                Prize3 = t.Prize3,
                EntryFee = t.EntryFee,
                City = t.City,
                ServerDateTime = nowUtc,
                StartDate = t.StartDate.Date,
                EndDate = t.EndDate.Date,
                IsJoined = joinedIds.Contains(t.TournamentId)
            }).ToList();

            return result;
        }


        public async Task<TournamentDTO> JoinTournament(int tournamentId)
        {
            var playerId = GetCurrentPlayerId();
            using var ctx = _contextFactory.CreateDbContext();

            var balance = await _crypto.GetTotalBalanceAsync(playerId.ToString());
            var tournament = await ctx.Tournaments.FirstOrDefaultAsync(x => x.TournamentId == tournamentId);

            if (tournament == null)
            {
                return await BuildTournamentDto(ctx, tournament, playerId, "NOTFOUND");
            }

            if (tournament.EntryFee > balance)
            {
                return await BuildTournamentDto(ctx, tournament, playerId, "INSUFFICIENT_BALANCE");
            }

            var existingChallenger = await ctx.TournamentChallengers.FirstOrDefaultAsync(tc => tc.TournamentId == tournamentId && tc.PlayerId == playerId);

            bool isNewChallenger = false;

            if (existingChallenger != null)
            {
                switch (existingChallenger.Status)
                {
                    case "JOINEND":
                        return await BuildTournamentDto(ctx, tournament, playerId, "JOINEND");

                    case "FAILED":
                        existingChallenger.RetryCount++;
                        existingChallenger.Status = "JOINEND";
                        break;

                    case "NOTPAID":
                        existingChallenger.Status = "JOINEND";
                        break;

                    default:
                        return await BuildTournamentDto(ctx, tournament, playerId, "UNKNOWN_STATE");
                }
            }
            else
            {
                existingChallenger = new TournamentChallenger
                {
                    TournamentId = tournamentId,
                    PlayerId = playerId,
                    RetryCount = 1,
                    Status = "JOINEND"
                };
                isNewChallenger = true;
            }

            var debited = await _crypto.DebitToMasterOffChainAsync(playerId.ToString(), tournament.EntryFee);
            if (!debited)
            {
                if (!isNewChallenger)
                {
                    existingChallenger.Status = "NOTPAID";
                    ctx.TournamentChallengers.Update(existingChallenger);
                }

                await ctx.SaveChangesAsync();
                return await BuildTournamentDto(ctx, tournament, playerId, "NOTPAID");
            }

            if (isNewChallenger)
                await ctx.TournamentChallengers.AddAsync(existingChallenger);
            else
                ctx.TournamentChallengers.Update(existingChallenger);

            await ctx.SaveChangesAsync();
            return await BuildTournamentDto(ctx, tournament, playerId, "JOINEND");
        }



        private async Task<TournamentDTO> BuildTournamentDto(LudoDbContext ctx, Tournament tournament, int playerId, String StatusCode = "SUCCESS")
        {
            var joinedIds = await ctx.TournamentChallengers
                .Where(tc => tc.PlayerId == playerId)
                .Select(tc => tc.TournamentId)
                .ToHashSetAsync();

            return new TournamentDTO
            {
                TournamentId = tournament.TournamentId,
                Name = tournament.Name,
                Winner1 = tournament.Winner1,
                Winner2 = tournament.Winner2,
                Winner3 = tournament.Winner3,
                Prize1 = tournament.Prize1,
                Prize2 = tournament.Prize2,
                Prize3 = tournament.Prize3,
                EntryFee = tournament.EntryFee,
                City = tournament.City,
                ServerDateTime = DateTime.Now,
                StartDate = tournament.StartDate.Date,
                EndDate = tournament.EndDate.Date,
                IsJoined = joinedIds.Contains(tournament.TournamentId),
                StatusCode = StatusCode
            };
        }
        /* END TOURNAMENT API */
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