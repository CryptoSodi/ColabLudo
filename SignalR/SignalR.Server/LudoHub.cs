using Google.Apis.Auth;
using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SharedCode;
using SharedCode.Constants;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace SignalR.Server
{// A simple command class that holds details for a command.
    
    public class LudoHub : Hub
    {
        // Thread-safe connection mappings
        public static ConcurrentDictionary<int, string> PlayerToConnection = new ConcurrentDictionary<int, string>();
        public static ConcurrentDictionary<string, int> ConnectionToPlayer = new ConcurrentDictionary<string, int>();

        private readonly IDbContextFactory<LudoDbContext> _contextFactory;
        private readonly IHubContext<LudoHub> _hubContext;
        public static CryptoHelper _crypto;
        public static DatabaseManager DM;
        private static bool _initialized = false;

        public LudoHub(IDbContextFactory<LudoDbContext> contextFactory, IHubContext<LudoHub> hubContext, CryptoHelper crypto)
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
        private PlayerInfo CastPlayerToInfo(Player player)
        {
            PlayerInfo playerInfo = new PlayerInfo
            {
                PlayerId = player.PlayerId,
                Name = player.Name,
                Email = player.Email,
                PictureUrl = player.PictureUrl,
                PhoneNumber = player.PhoneNumber,
                City = player.City,
                CountryCode = player.CountryCode,
                IsOnline = player.IsOnline,
                AuthToken = _crypto.Encrypt(player.PlayerId.ToString()), // or a JWT with playerId claim
                GamesPlayed = player.GamesPlayed,
                GamesWon = player.GamesWon,
                GamesLost = player.GamesLost,
                BestWin = player.BestWin,
                TotalWin = player.TotalWin,
                TotalLost = player.TotalLost,
                Score = player.Score,
                Wallets = player.Wallets.Select(w => new SharedCode.Constants.PlayerWallet
                {
                    PlayerId = w.PlayerId,
                    AddressType = w.AddressType,
                    WalletAddress = w.WalletAddress,
                    AvailableBalance = w.AvailableBalance
                }).ToList()
            };
            return playerInfo;
        }
        public async Task<PlayerInfo> GoogleAuthentication(string idToken, string city, string countryCode)
        {
            using var ctx = _contextFactory.CreateDbContext();
            // Example: extract useful info
            String email = "";
            String name = "";
            String pictureUrl = "";// ✅ profile picture URL
            String googleId = "";// Unique Google user ID
            if (idToken == "Guest1")
            {
                email = "Sodi@gmail.com";
                name = "Sodi";
                pictureUrl = "https://yt3.ggpht.com/ytc/AIdro_nuNlfceTDiBSTQUhxQ56YDJFbBu1DjRfTpJMFP6ck9D0x3tsglom8eMUA2blBLpRVU8w=s108-c-k-c0x00ffffff-no-rj";// ✅ profile picture URL
                googleId = idToken;
            }
            else
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);

                // ✅ Validate issuer and audience (REPLACE with your real Google OAuth client ID)
                var expectedAudience = "973406093603-g14f7hkjafphcij4p16ectibrkmj7q8f.apps.googleusercontent.com";
                //973406093603-g14f7hkjafphcij4p16ectibrkmj7q8f.apps.googleusercontent.com
                if (payload.Audience+"" != expectedAudience || (payload.Issuer != "accounts.google.com" && payload.Issuer != "https://accounts.google.com"))
                {
                    return null;
                }

                // Example: extract useful info
                email = payload.Email;
                name = payload.Name;
                pictureUrl = payload.Picture; // ✅ profile picture URL
                googleId = payload.Subject; // Unique Google user ID
            }

            Player existingPlayer = ctx.Players.FirstOrDefault(p => p.GoogleId == googleId);            
            // If player exists by email, return with Player Id to login
            if (existingPlayer != null)
            {
                existingPlayer.AuthToken = _crypto.Encrypt(existingPlayer.PlayerId.ToString()); // or a JWT with playerId claim
                SetPlayerOnlineState(existingPlayer.PlayerId, true).GetAwaiter().GetResult();
                
                // 1) Store SignalR connection
                PlayerToConnection[existingPlayer.PlayerId] = Context.ConnectionId;
                ConnectionToPlayer[Context.ConnectionId] = existingPlayer.PlayerId;                
                existingPlayer.Wallets = new List<LudoServer.Models.PlayerWallet>
                {
                    new LudoServer.Models.PlayerWallet
                    {
                        PlayerId = existingPlayer.PlayerId,
                        AddressType = "SOL",
                        WalletAddress = await _crypto.GetOrCreateAccount(existingPlayer.PlayerId),
                        AvailableBalance = await _crypto.GetOffChainBalanceAsync(existingPlayer.PlayerId)
                    }
                };
                
                await ctx.SaveChangesAsync();
                return CastPlayerToInfo(existingPlayer);
            }

            Player newPlayer = new Player
            {
                GoogleId = googleId,
                Name = name,
                Email = email,
                PictureUrl = pictureUrl,
                City = city,
                CountryCode = countryCode,
                IsOnline = true,
                AuthToken = ""
            };
            ctx.Players.Add(newPlayer);
            // Save changes to the database
            await ctx.SaveChangesAsync();

            existingPlayer = ctx.Players.FirstOrDefault(p => p.GoogleId == googleId);
            
            if (existingPlayer != null)
            {
                existingPlayer.AuthToken = _crypto.Encrypt(existingPlayer.PlayerId.ToString());
                PlayerToConnection[existingPlayer.PlayerId] = Context.ConnectionId;
                ConnectionToPlayer[Context.ConnectionId] = existingPlayer.PlayerId;
                
                existingPlayer.Wallets = new List<LudoServer.Models.PlayerWallet>
                {
                    new LudoServer.Models.PlayerWallet
                    {
                        PlayerId = existingPlayer.PlayerId,
                        AddressType = "SOL",
                        WalletAddress = await _crypto.GetOrCreateAccount(existingPlayer.PlayerId),
                        AvailableBalance = await _crypto.GetOffChainBalanceAsync(existingPlayer.PlayerId)
                    }
                };

                await ctx.SaveChangesAsync();
                return CastPlayerToInfo(existingPlayer);
            }
            // If player creation failed, return null
            return null;
        }
        // Call this once after authentication or lobby-join to establish mapping.
        public async Task<PlayerInfo> UserConnectedSetID(String AuthToken)
        {
            try
            {
                int playerId = int.Parse(_crypto.Decrypt(AuthToken));
            
                if (playerId == -1)
                    return null;
                // 1) Store SignalR connection
                PlayerToConnection[playerId] = Context.ConnectionId;
                ConnectionToPlayer[Context.ConnectionId] = playerId;
                return CastPlayerToInfo(await GetCallerPlayer());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in {AuthToken} : UserConnectedSetID: {ex.Message}");
            }
            return null;
        }
        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"User connected: {Context.ConnectionId}");

            if (ConnectionToPlayer.TryGetValue(Context.ConnectionId, out var playerId))
            {
                PlayerToConnection[playerId] = Context.ConnectionId;
                await SetPlayerOnlineState(playerId, true);
            }

            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Player player = await GetCallerPlayer();
            await SetPlayerOnlineState(player.PlayerId, false);

            if (ConnectionToPlayer.TryRemove(Context.ConnectionId, out var playerId))
            {
                PlayerToConnection.TryRemove(playerId, out _);
                
            }
            await base.OnDisconnectedAsync(exception);
        }
        /// Helper to fetch the current caller's player ID from the connection map.
        private async Task<Player> GetCallerPlayer()
        {
            if (ConnectionToPlayer.TryGetValue(Context.ConnectionId, out var playerId))
            {
                using var ctx = _contextFactory.CreateDbContext();
                Player sender = ctx.Players.Find(playerId);
                
                sender.Wallets = new List<LudoServer.Models.PlayerWallet>
                {
                    new LudoServer.Models.PlayerWallet
                    {
                        PlayerId = sender.PlayerId,
                        AddressType = "SOL",
                        WalletAddress = await _crypto.GetOrCreateAccount(sender.PlayerId),
                        AvailableBalance = await _crypto.GetOffChainBalanceAsync(sender.PlayerId)
                    }
                };

                if (sender == null)
                    throw new HubException("Player not recognized.");
                return sender;
            }   
            throw new HubException("Player not recognized.");
        }
        public async Task<String> SendSol(string destination, decimal amountInSol)
        {
            Player player = await GetCallerPlayer();

            try
            {
                var txSignature = await _crypto.SendFromMasterAsync(destination, amountInSol);

                // 0) Check total balance (on-chain + off-chain)
                var totalBalance = await _crypto.GetTotalBalanceAsync(player.PlayerId);
                if (totalBalance < amountInSol)
                {
                    Console.WriteLine($"Withdrawal failed: insufficient total balance for {player.PlayerId}. Have {totalBalance} SOL, tried {amountInSol} SOL.");
                    return "INSUFFICIENT_FUNDS";
                }

                // 1) Debit from off-chain ledger (credit master balance)
                var debited = await _crypto.OffChainTransaction(player.PlayerId, -amountInSol, "Withdraw", txSignature, true);
                if (!debited)
                {
                    Console.WriteLine($"Withdrawal failed: insufficient off-chain funds for {player.PlayerId}");
                    return "INSUFFICIENT_OFFCHAIN";
                }

                // 2) Send on-chain using master wallet
                
                Console.WriteLine($"Withdrawal of {amountInSol} SOL for {player.PlayerId} sent from master. Tx: {txSignature}");
                return txSignature;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during withdrawal for {player.PlayerId}: {ex.Message}");
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
        public async Task LeaveCloseLobby(string roomCode)
        {
            Player player = await GetCallerPlayer();
            if (roomCode != null)
                try
                {
                    var (existingGame, user) = await DM.LeaveGameLobby(Context.ConnectionId, player.PlayerId, roomCode);
                    // Optionally, perform additional cleanup or update the game engine state.
                    // For example: engine.RemoveUser(user); // if your engine supports this
                    if (user != null)
                    {
                        await _hubContext.Clients.Group(roomCode).SendAsync("PlayerLeft", user.PlayerColor);
                    }
                    else
                    {
                        Console.WriteLine("User is null in LeaveLobby");
                    }
                    // Notify all connected clients that a user has left.
                    await BroadcastPlayersAsync(existingGame);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"LeaveLobby error: {ex.Message}");
                }
        }
        private async Task gameStartAsync(Game existingGame)
        {
            using var context = _contextFactory.CreateDbContext();
            List<SharedCode.PlayerDto> seats = new List<SharedCode.PlayerDto>();
            List<Player> playerList = new List<Player>();
            if (existingGame.MultiPlayer.P1 != null)
            {
                Player P = await context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P1);
                playerList.Add(P);
                seats.Add(new SharedCode.PlayerDto { PlayerId = P.PlayerId, PlayerName = P.Name, PlayerPicture = P.PictureUrl, PlayerColor = "Red" });                
            }
            if (existingGame.MultiPlayer.P2 != null)
            {
                Player P = await context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P2);
                playerList.Add(P);
                if (existingGame.GameType == "2")
                    seats.Add(new SharedCode.PlayerDto { PlayerId = P.PlayerId, PlayerName = P.Name, PlayerPicture = P.PictureUrl, PlayerColor = "Yellow" });
                else
                    seats.Add(new SharedCode.PlayerDto { PlayerId = P.PlayerId, PlayerName = P.Name, PlayerPicture = P.PictureUrl, PlayerColor = "Green" });
            }
            if (existingGame.MultiPlayer.P3 != null)
            {
                Player P = await context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P3);
                playerList.Add(P);
                seats.Add(new SharedCode.PlayerDto { PlayerId = P.PlayerId, PlayerName = P.Name, PlayerPicture = P.PictureUrl, PlayerColor = "Yellow" });
            }
            if (existingGame.MultiPlayer.P4 != null)
            {
                Player P = await context.Players.FirstOrDefaultAsync(p => p.PlayerId == existingGame.MultiPlayer.P4);
                playerList.Add(P);
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
                {
                    gameRoom.Users[i].PlayerColor = seats[i].PlayerColor.ToLower();
                    gameRoom.Users[i].AuthToken = playerList[i].AuthToken;
                }
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
        public async Task<GameCommand> Send(string AuthToken, GameCommand commandValue, string commandtype, string roomCode)
        {
            Player player = await GetCallerPlayer();
            if (player.AuthToken != AuthToken)
                return null;
            
            GameCommand Result = new GameCommand();
            Console.WriteLine($"{player.Name}: {commandValue}:{commandtype}");
            // Now use the user's Room property to get the GameRoom.
            if (!DM._gameRooms.TryGetValue(roomCode, out GameRoom gameRoom))
            {
                Console.WriteLine($"GameRoom not found for room: {roomCode}");
                //
                Result.Result = "Error: Room not found.";
                return Result;
            }
            // For logging purposes, show which room this command is coming from.
            Console.WriteLine($"{player.Name} (room {roomCode}): {commandValue}:{commandtype}");
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
                Result = gameRoom.MovePieceAsync(AuthToken, commandValue).GetAwaiter().GetResult();
                return Result;
            }
            else if (commandtype == "DiceRoll")
            {
                // For other command types, for example, SeatTurn:
                // If SeatTurn returns a string, you can wait for it.
                Result = gameRoom.SeatTurn(AuthToken, commandValue).GetAwaiter().GetResult();
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
                {
                    CM.Index = gameRoom.chatMessages.Count;
                    gameRoom.chatMessages.Add(CM);
                }
                List<User> otherUsers = gameRoom.Users.Where(p => p.PlayerId != CM.SenderId).ToList();
                User senderUser = gameRoom.Users.Where(p => p.PlayerId == CM.SenderId).ToList()[0];
                CM.SenderColor = senderUser.PlayerColor;

                foreach (User u in otherUsers)
                {
                    if (CM.Message != "")
                        Clients.Client(PlayerToConnection[u.PlayerId]).SendAsync("ReceiveChatHistory", CM);
                }
                return gameRoom.chatMessages.Take(20).ToList();
            }
            else 
            {
                using var context = _contextFactory.CreateDbContext();
                if(CM.Message != "")
                {
                    // 1️⃣ Save the new message to the database                    
                    ChatMessage newMessage = new ChatMessage
                    {
                        SenderId = CM.SenderId,
                        SenderName = CM.SenderName,
                        SenderColor = CM.SenderColor,
                        SenderPicture = CM.SenderPicture,
                        ReceiverId = CM.ReceiverId,
                        ReceiverName = CM.ReceiverName,
                        Message = CM.Message,
                        CreatedDate = DateTime.UtcNow  // Set the timestamp here
                    };
                    context.ChatMessages.Add(newMessage);
                    context.SaveChanges();
                }

                List<ChatMessage> chatHistory = context.ChatMessages.Where(cm => 
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
                    CreatedDate = cm.CreatedDate
                }).ToList();

                //chatMessagesList.Add(CM);
                // Optionally, also send back the last 50 messages to the sender
                // send only to the receiver
                if (PlayerToConnection.TryGetValue(CM.ReceiverId, out var connId) && CM.Message != "")
                {
                    Clients.Client(connId).SendAsync("ReceiveChatHistory", CM);
                }
                return chatMessagesList.Take(30).ToList();
            }
        }
        /* END CHAT AND FRIENDS MANAGEMENT */        
        /* DAILY BONUS */
        public async Task<DailyBonusDto> GetDailyBonus()
        {
            Player player = await GetCallerPlayer();
            using var ctx = _contextFactory.CreateDbContext();

            // Fetch the record (or null)
            var bonus = await ctx.DailyBonus.FirstOrDefaultAsync(x => x.PlayerId == player.PlayerId);
            var now = DateTime.UtcNow;
            var today = now.Date;
            var weekdayIndex = (int)now.DayOfWeek; // Sunday=0, Monday=1, …

            if (bonus == null)
            {
                // First‐time setup
                bonus = new DailyBonus
                {
                    PlayerId = player.PlayerId,
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
            Player player = await GetCallerPlayer();
            using var ctx = _contextFactory.CreateDbContext();

            var bonus = await ctx.DailyBonus.FirstOrDefaultAsync(x => x.PlayerId == player.PlayerId);
            var today = DateTime.UtcNow.Date;
            var weekdayIndex = (int)DateTime.UtcNow.DayOfWeek; // Sunday=0, Monday=1, …

            if (bonus == null)
            {
                // Initialize record if missing
                bonus = new DailyBonus
                {
                    PlayerId = player.PlayerId,
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
            Player player = GetCallerPlayer().GetAwaiter().GetResult();
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
                .Where(tc => tc.PlayerId == player.PlayerId)
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
            Player player = await GetCallerPlayer();
            using var ctx = _contextFactory.CreateDbContext();

            var balance = await _crypto.GetOffChainBalanceAsync(player.PlayerId);
            var tournament = await ctx.Tournaments.FirstOrDefaultAsync(x => x.TournamentId == tournamentId);

            if (tournament == null)
            {
                return await BuildTournamentDto(ctx, tournament, player.PlayerId, "NOTFOUND");
            }

            if (tournament.EntryFee > balance)
            {
                return await BuildTournamentDto(ctx, tournament, player.PlayerId, "INSUFFICIENT_BALANCE");
            }

            var existingChallenger = await ctx.TournamentChallengers.FirstOrDefaultAsync(tc => tc.TournamentId == tournamentId && tc.PlayerId == player.PlayerId);

            bool isNewChallenger = false;

            if (existingChallenger != null)
            {
                switch (existingChallenger.Status)
                {
                    case "JOINEND":
                        return await BuildTournamentDto(ctx, tournament, player.PlayerId, "JOINEND");

                    case "FAILED":
                        existingChallenger.RetryCount++;
                        existingChallenger.Status = "JOINEND";
                        break;

                    case "NOTPAID":
                        existingChallenger.Status = "JOINEND";
                        break;

                    default:
                        return await BuildTournamentDto(ctx, tournament, player.PlayerId, "UNKNOWN_STATE");
                }
            }
            else
            {
                existingChallenger = new TournamentChallenger
                {
                    TournamentId = tournamentId,
                    PlayerId = player.PlayerId,
                    RetryCount = 1,
                    Status = "JOINEND"
                };
                isNewChallenger = true;
            }

            var debited = await _crypto.OffChainTransaction(player.PlayerId, -tournament.EntryFee, "Tounrnament Fee");
            if (!debited)
            {
                if (!isNewChallenger)
                {
                    existingChallenger.Status = "NOTPAID";
                    ctx.TournamentChallengers.Update(existingChallenger);
                }

                await ctx.SaveChangesAsync();
                return await BuildTournamentDto(ctx, tournament, player.PlayerId, "NOTPAID");
            }

            if (isNewChallenger)
                await ctx.TournamentChallengers.AddAsync(existingChallenger);
            else
                ctx.TournamentChallengers.Update(existingChallenger);

            await ctx.SaveChangesAsync();
            return await BuildTournamentDto(ctx, tournament, player.PlayerId, "JOINEND");
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
                ServerDateTime = DateTime.UtcNow,
                StartDate = tournament.StartDate.Date,
                EndDate = tournament.EndDate.Date,
                IsJoined = joinedIds.Contains(tournament.TournamentId),
                StatusCode = StatusCode
            };
        }
        /* END TOURNAMENT API */
        /* FRIENDS API */
        public async Task<List<PlayerCard>> GetFriends(String Type="All") {
            List<PlayerCard> result = new List<PlayerCard>();
            using var ctx = _contextFactory.CreateDbContext();
            Player player = await GetCallerPlayer();

            // First, get the last game players
            var lastGame = ctx.MultiPlayers
                .Where(m => m.P1 == player.PlayerId || m.P2 == player.PlayerId || m.P3 == player.PlayerId || m.P4 == player.PlayerId)
                .OrderByDescending(m => m.MultiPlayerId)
                .FirstOrDefault();

            if (lastGame != null)
            {
                var playerIds = new List<int?> { lastGame.P1, lastGame.P2, lastGame.P3, lastGame.P4 }
                    .Where(id => id.HasValue && id.Value != player.PlayerId)
                    .Select(id => id.Value)
                    .ToList();

                if (playerIds.Any())
                {
                    var lastGamePlayers = ctx.Players
                        .Where(p => playerIds.Contains(p.PlayerId))
                        .Select(p => new PlayerCard
                        {
                            playerID = p.PlayerId,
                            name = p.Name,
                            pictureUrl = p.PictureUrl,
                            rank = 31, // No rank info available, setting 0 or you can later compute
                            status = "",
                            lastGame = true
                        })
                        .ToList();

                    result.AddRange(lastGamePlayers);
                }
            }
            // Now, get friends (all statuses)
            var friends = ctx.FriendsRequests
                .Where(fr => fr.SenderId == player.PlayerId || fr.ReceiverId == player.PlayerId)
                .Select(fr => new
                {
                    OtherPlayer = fr.SenderId == player.PlayerId ? fr.Receiver : fr.Sender,
                    Status = fr.Status
                }).ToList();


            foreach (var fr in friends)
            {
                // try to find an existing card (e.g. from lastGame)
                var existing = result.FirstOrDefault(x => x.playerID == fr.OtherPlayer.PlayerId);

                if (existing != null)
                {
                    // update the status (and sender/receiver flag) on the existing card
                    existing.status = fr.Status.ToString();
                }
                else
                {
                    // still need to add brand-new friends
                    result.Add(new PlayerCard
                    {
                        playerID = fr.OtherPlayer.PlayerId,
                        name = fr.OtherPlayer.Name,
                        pictureUrl = fr.OtherPlayer.PictureUrl,
                        rank = 31,
                        status = fr.Status.ToString(),
                        lastGame = false
                    });
                }
            }
            foreach (var fr in result)
            {       // update the status (and sender/receiver flag) on the existing card
                if (fr.status.ToString() == "")
                    fr.status = "UN FRIEND";
            }
            if (!result.Any())
                return new List<PlayerCard>();
            return result;
        }
        public string SendFriendRequest(int ReceiverId, string status)
        {
            using var ctx = _contextFactory.CreateDbContext();
            Player player =  GetCallerPlayer().GetAwaiter().GetResult();

            if (player.PlayerId == ReceiverId)
                return "Cannot send friend request to yourself.";
            
            var receiver = ctx.Players.Find(ReceiverId);

            if (player == null || receiver == null)
                return "Sender or Receiver not found.";

            //var existingRequest = _context.FriendsRequests
            //    .FirstOrDefault(fr =>
            //        (fr.SenderId == SenderId && fr.ReceiverId == ReceiverId) ||
            //        (fr.SenderId == ReceiverId && fr.ReceiverId == SenderId));

            //if (existingRequest != null)
            //    return Conflict(new { Message = "Friend request already exists or is pending." });

            FriendRequest request = new FriendRequest();
            request.Status = status;
            request.CreatedDate = DateTime.UtcNow;

            // Make sure navigation properties are not set by client
            request.SenderId = player.PlayerId;
            request.ReceiverId = ReceiverId;

            ctx.FriendsRequests.Add(request);
            ctx.SaveChanges();
            return status;
            //switch (status)
            //{
            //    case "UN BLOCK":
            //        return Ok(new { Message = "UN BLOCK" });
            //        break;
            //    case "BLOCK":
            //        return Ok(new { Message = "BLOCK" });
            //        break;
            //    case "FIREND":
            //        return Ok(new { Message = "PENDING" });
            //        break;
            //    case "UN FRIEND":
            //        return Ok(new { Message = "UN FRIEND" });
            //        break;
            //}
        }
        /* END FRIENDS API */
        public async Task<string> CreateJoinLobby(SharedCode.GameDto gameDTO)
        {
            Player player = await GetCallerPlayer();
            Game gameRoom = await DM.JoinGameLobby(Context.ConnectionId, player, gameDTO);

            if (gameRoom == null)
            {
                return "Room is full";
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, gameRoom.RoomCode);
            await BroadcastPlayersAsync(gameRoom);
            return gameRoom.RoomCode; // Return the room name to the client
        }
        /// Gets a list of all games.
        public async Task<List<Game>> GetGame(bool IsPrivate)
        {
            using var ctx = _contextFactory.CreateDbContext();
            //g.State == "Active"
            return await ctx.Games.Where(g => g.State == "Active" && g.IsPrivate == IsPrivate && !g.IsPractice).ToListAsync();
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
        // 2. Update the player's IsOnline state in the DB
        private async Task SetPlayerOnlineState(int playerId, bool isOnline)
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                var player = await ctx.Players.FirstOrDefaultAsync(p => p.PlayerId == playerId);
                if (player != null)
                {
                    player.IsOnline = isOnline;
                    await ctx.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating online status for player {playerId}: {ex.Message}");
            }
        }
    }
    public class User
    {
        public User(string connectionId, string roomCode, int playerId, string userName, string playerColor)
        {
            this.ConnectionId = connectionId;
            this.roomCode = roomCode;
            this.PlayerId = playerId;
            this.PlayerName = userName;
            this.PlayerColor = playerColor;
        }
        public string ConnectionId { get; init; }
        public string roomCode { get; init; }
        public int PlayerId { get; init; }
        public string PlayerName { get; init; }
        public string PlayerColor { get; set; }  // Now mutable
        public string AuthToken { get; set; }  // Now mutable
    }
}