using Google.Apis.Auth;
using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SharedCode;
using SharedCode.Constants;
using SignalR.Server.Services;
using System.Collections.Concurrent;

namespace SignalR.Server
{// A simple command class that holds details for a command.
    
    public class LudoHub : Hub
    {
        // Thread-safe connection mappings
        public static ConcurrentDictionary<int, string> PlayerToConnection = new ConcurrentDictionary<int, string>();
        public static ConcurrentDictionary<string, int> ConnectionToPlayer = new ConcurrentDictionary<string, int>();

        private readonly IDbContextFactory<LudoDbContext> _contextFactory;
        private readonly IHubContext<LudoHub> _hubContext;
        private FriendsService _friendsService;
        private TournamentService _tournamentService;
        private DailyBonusService _dailyBonusService;
        private GoogleAuthService _googleAuthService;
        private UtilService _utilService;
        public static CryptoHelper _crypto;

        public static DatabaseManager DM;
        private static bool _initialized = false;

        public LudoHub(IDbContextFactory<LudoDbContext> contextFactory, IHubContext<LudoHub> hubContext, CryptoHelper crypto, FriendsService friendsService, TournamentService tournamentService, DailyBonusService dailyBonusService, GoogleAuthService googleAuthService, UtilService utilService)
        {
            _friendsService = friendsService;
            _tournamentService = tournamentService;
            _dailyBonusService = dailyBonusService;
            _googleAuthService = googleAuthService;
            _utilService = utilService;
            _crypto = crypto;
            _contextFactory = contextFactory;
            _hubContext = hubContext;
            if (!_initialized)
            {
                _initialized = true;
                DM = new DatabaseManager(_hubContext, _contextFactory, _crypto);
            }
        }
        public async Task<PlayerInfo> GoogleAuthentication(string idToken, string city, string countryCode)
        {
           var player = await _googleAuthService.GoogleAuthentication(idToken, city, countryCode);

            if (player != null)
            {
                using var ctx = _contextFactory.CreateDbContext();
                player.AuthToken = _crypto.Encrypt(player.PlayerId.ToString()); // or a JWT with playerId claim
                PlayerToConnection[player.PlayerId] = Context.ConnectionId;
                ConnectionToPlayer[Context.ConnectionId] = player.PlayerId;
                _utilService.SetPlayerOnlineState(player.PlayerId, true).GetAwaiter().GetResult();
                await ctx.SaveChangesAsync();
                return await _utilService.CastPlayerToInfoAsync(player);
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
                return await _utilService.CastPlayerToInfoAsync(await GetCallerPlayer());
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
                await _utilService.SetPlayerOnlineState(playerId, true);
            }

            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Player player = await GetCallerPlayer();
            await _utilService.SetPlayerOnlineState(player.PlayerId, false);
            await LeaveCloseLobby();
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
                var wal = await ctx.PlayerWallet.FirstOrDefaultAsync(p => p.PlayerId == playerId);

                sender.Wallets = new List<LudoServer.Models.PlayerWallet>
                {
                    new LudoServer.Models.PlayerWallet
                    {
                        PlayerId = sender.PlayerId,
                        AddressType = wal.AddressType,
                        WalletAddress = wal.WalletAddress,
                        AvailableBalance = wal.AvailableBalance
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
            var r = await _crypto.SendSolToExternalWallet(player, destination, amountInSol);
            await Clients.Caller.SendAsync("PlayerInfoUpdate", await _utilService.CastPlayerToInfoAsync(player));
            return r;
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
        public async Task LeaveCloseLobby()
        {
            Player player = await GetCallerPlayer();
            try
            {
                var (existingGame, user) = await DM.LeaveGameLobby(Context.ConnectionId, player.PlayerId);
                // Optionally, perform additional cleanup or update the game engine state.
                // For example: engine.RemoveUser(user); // if your engine supports this
                if (user != null && existingGame!=null)
                {
                    await _hubContext.Clients.Group(existingGame.RoomCode).SendAsync("PlayerLeft", user.PlayerColor);
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
            try
            {
                await Clients.Caller.SendAsync("PlayerInfoUpdate", await _utilService.CastPlayerToInfoAsync(player));
            }
            catch (Exception)
            {
            }
        }
        public async Task<string> Ready()
        {
            using var context = _contextFactory.CreateDbContext();
            Player player = await GetCallerPlayer();
            // Find the game where this player exists
            Game existingGame = DM.games.FirstOrDefault(g =>
                g.State == "Active" &&
                (g.MultiPlayer.P1 == player.PlayerId ||
                 g.MultiPlayer.P2 == player.PlayerId ||
                 g.MultiPlayer.P3 == player.PlayerId ||
                 g.MultiPlayer.P4 == player.PlayerId));

            if (existingGame == null)
            {
                Console.WriteLine("No active game found for this player.");
                return "No active game found.";
            }

            await BroadcastPlayersAsync(existingGame);
            // Build playerId-to-color mapping
            var playerSlots = new (int? PlayerId, string Color)[]
            {
                (existingGame.MultiPlayer.P1, "Red"),
                (existingGame.MultiPlayer.P2, existingGame.GameType == "2" ? "Yellow" : "Green"),
                (existingGame.MultiPlayer.P3, "Yellow"),
                (existingGame.MultiPlayer.P4, "Blue")
            };
            // Get all player IDs that are not null
            var playerIds = playerSlots.Where(slot => slot.PlayerId.HasValue).Select(slot => slot.PlayerId.Value).ToList();
            // Fetch all players in a single query
            var players = await context.Players.Where(p => playerIds.Contains(p.PlayerId)).ToListAsync();

            // Build PlayerDto and Player lists
            List<SharedCode.PlayerDto> seats = new();
            List<Player> playerList = new();
            foreach (var (playerId, color) in playerSlots.Where(s => s.PlayerId.HasValue))
            {
                var playerSub = players.FirstOrDefault(p => p.PlayerId == playerId.Value);
                if (playerSub != null)
                {
                    playerList.Add(playerSub);
                    seats.Add(new SharedCode.PlayerDto
                    {
                        PlayerId = playerSub.PlayerId,
                        PlayerName = playerSub.Name,
                        PlayerPicture = playerSub.PictureUrl,
                        PlayerColor = color
                    });
                }
            }

            // Check if game is full
            if (existingGame.GameType == seats.Count.ToString() || (seats.Count == 4 && existingGame.GameType == "22"))
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
                using var ctx = _contextFactory.CreateDbContext();
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
                    ctx.ChatMessages.Add(newMessage);
                    ctx.SaveChanges();
                }

                List<ChatMessage> chatHistory = ctx.ChatMessages.Where(cm => 
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
            return await _dailyBonusService.GetDailyBonus(player);
        }
        // New function: Claim today's bonus and update LastResetDate
        public async Task<DailyBonusDto> ClaimTodayBonus()
        {
            Player player = await GetCallerPlayer();
            var r = await _dailyBonusService.ClaimTodayBonus(player);
            await Clients.Caller.SendAsync("PlayerInfoUpdate", await _utilService.CastPlayerToInfoAsync(player));
            return r;
        }
        /* END DAILY BONUS */
        /* TOURNAMENT API */
        public async Task<TournamentResultDTO> GetResultsTournament(int tournamentId)
        {
            return await _tournamentService.GetResultsTournament(tournamentId);
        }
        public async Task<List<TournamentDTO>> GetAllTournaments(string type)
        {
            Player player = GetCallerPlayer().GetAwaiter().GetResult();
            return await _tournamentService.GetAllTournaments(player, type);
        }
        public async Task<TournamentDTO> JoinTournament(int tournamentId)
        {
            Player player = GetCallerPlayer().GetAwaiter().GetResult();
            var r = await _tournamentService.JoinTournament(player, tournamentId);
            await Clients.Caller.SendAsync("PlayerInfoUpdate", await _utilService.CastPlayerToInfoAsync(player));
            return r;
        }
        /* END TOURNAMENT API */
        /* FRIENDS API */
        public async Task<List<PlayerCard>> GetFriends(string type = "All")
        {
            var player = await GetCallerPlayer(); // Assume async
            return await _friendsService.GetFriends(player, type);
        }
        public async Task<string> SendFriendRequest(int receiverId, string status)
        {
            var player = await GetCallerPlayer();
            return await _friendsService.SendFriendRequest(player, receiverId, status);
        }
        /* END FRIENDS API */
        public async Task<string> CreateJoinLobby(SharedCode.GameDto gameDTO)
        {
            Player player = await GetCallerPlayer();
            Game gameRoom = await DM.JoinGameLobby(Context.ConnectionId, player, gameDTO);
            try
            {
                await Clients.Caller.SendAsync("PlayerInfoUpdate", await _utilService.CastPlayerToInfoAsync(player));
            }
            catch (Exception){}
            if (gameRoom == null){
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
            if (existingGame == null)
                return;
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