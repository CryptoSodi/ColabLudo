using LudoServer.Data;
using LudoServer.Models;
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
        public static ConcurrentDictionary<string, Player> ConnectionToPlayer = new ConcurrentDictionary<string, Player>();

        private readonly IDbContextFactory<LudoDbContext> _contextFactory;
        private readonly IHubContext<LudoHub> _hubContext;
        private FriendsService _friendsService;
        private TournamentService _tournamentService;
        private DailyBonusService _dailyBonusService;
        private GoogleAuthService _googleAuthService;
        private UtilService _utilService;
        public static CryptoHelper _crypto;

        public static DatabaseManager DM { get; set; }
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
            // Initialize the DatabaseManager only once
            if (!_initialized)
            {
                _initialized = true;
                DM = new DatabaseManager(_hubContext, _contextFactory, _crypto, _utilService);
                Task.Run(DM.LoadData);
            }
        }
        public async Task<PlayerInfo> GoogleAuthentication(string idToken, string city, string countryCode)
        {
            try
            {
                var player = await _googleAuthService.GoogleAuthentication(idToken, city, countryCode);
                ConnectionToPlayer[Context.ConnectionId] 
                     = await _utilService.GetPlayerByID(player.PlayerId);
                       await _utilService.SetPlayerOnlineState(player.PlayerId, true);
                return await _utilService.CastPlayerToInfoAsync(player);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Authentication : {ex.Message} ");
                // If player creation failed, return null
                return null;
            }
        }
        // Call this once after authentication or lobby-join to establish mapping.
        public async Task<PlayerInfo> UserConnectedSetID(String AuthToken)
        {
            // 1) Store SignalR connection
            try
            {
                ConnectionToPlayer[Context.ConnectionId] = await _utilService.GetPlayerByID(int.Parse(_crypto.Decrypt(AuthToken)));
                return await _utilService.CastPlayerToInfoAsync(ConnectionToPlayer[Context.ConnectionId]);
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
            if (ConnectionToPlayer.TryGetValue(Context.ConnectionId, out var playerAtConnection))
                await _utilService.SetPlayerOnlineState(playerAtConnection.PlayerId, true);
            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await LeaveCloseLobby();
            if (ConnectionToPlayer.TryRemove(Context.ConnectionId, out var playerAtConnection))
            {
                await _utilService.SetPlayerOnlineState(playerAtConnection.PlayerId, false);
            }
            await base.OnDisconnectedAsync(exception);
        }
        /// Helper to fetch the current caller's player ID from the connection map.
        private async Task<Player> GetCallerPlayer()
        {
            if (ConnectionToPlayer.TryGetValue(Context.ConnectionId, out var player))
            {
                if (player == null)
                    throw new HubException("Player not recognized.");
                return player;
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
                var (existingGame, user) = await DM.LeaveGameLobby(player.PlayerId);
                // Optionally, perform additional cleanup or update the game engine state.                
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
            try
            {
                Player player = await GetCallerPlayer();
                // Find the game where this player exists
                Game existingGame = DM.GetActiveGame("Active", player.PlayerId);
                var (existingGameReady, seats, rollsString) = await DM.Ready(existingGame);
                await BroadcastPlayersAsync(existingGameReady);
                if (existingGameReady != null && seats != null && rollsString != "")
                {
                    await Clients.Group(existingGame.RoomCode).SendAsync("GameStarted", existingGame.GameType, JsonConvert.SerializeObject(seats), rollsString);
                    return "game started";
                }
                return "ready";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return $"failed at server: {ex.Message}";
            }
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
                List<User> otherUsers = gameRoom.Users.Where(p => p.player.PlayerId != CM.SenderId).ToList();
                User senderUser = gameRoom.Users.Where(p => p.player.PlayerId == CM.SenderId).ToList()[0];
                CM.SenderColor = senderUser.PlayerColor;

                foreach (User u in otherUsers)
                {
                    if (CM.Message != "")
                        Clients.Client(ConnectionToPlayer.FirstOrDefault(kv => kv.Value.PlayerId == u.player.PlayerId).Key).SendAsync("ReceiveChatHistory", CM);
                }
                return gameRoom.chatMessages.Take(20).ToList();
            }
            else
            {
                using var ctx = _contextFactory.CreateDbContext();
                if (CM.Message != "")
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
                if (CM.Message != "")
                    Clients.Client(ConnectionToPlayer.FirstOrDefault(kv => kv.Value.PlayerId == CM.ReceiverId).Key).SendAsync("ReceiveChatHistory", CM);
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
            Player player = await GetCallerPlayer();
            return await _tournamentService.GetAllTournaments(player, type);
        }
        public async Task<TournamentDTO> JoinTournament(int tournamentId)
        {
            Player player = await GetCallerPlayer();
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
            Game existingGame = await DM.JoinGameLobby(player, gameDTO);
            try
            {
                await Clients.Caller.SendAsync("PlayerInfoUpdate", await _utilService.CastPlayerToInfoAsync(player));
            }
            catch (Exception) { }
            if (existingGame == null)
            {
                return "Room is full";
            }
            await Groups.AddToGroupAsync(Context.ConnectionId, existingGame.RoomCode);
            await BroadcastPlayersAsync(existingGame);
            return existingGame.RoomCode; // Return the room name to the client
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

            using var ctx = _contextFactory.CreateDbContext();

            var seatInfos = new (string Seat, int? PlayerId)[]
            {
                ("P1", existingGame.MultiPlayer.P1),
                ("P2", existingGame.MultiPlayer.P2),
                ("P3", existingGame.MultiPlayer.P3),
                ("P4", existingGame.MultiPlayer.P4),
            };

            // Gather all non-null player IDs
            var playerIds = seatInfos.Where(x => x.PlayerId.HasValue).Select(x => x.PlayerId.Value).ToList();

            // Fetch all players in a single query
            var players = await ctx.Players.AsNoTracking().Where(p => playerIds.Contains(p.PlayerId)).ToListAsync();

            // Build a map for quick lookup
            var playerMap = players.ToDictionary(p => p.PlayerId);

            // Broadcast to all seats
            foreach (var (seat, playerId) in seatInfos)
            {
                if (playerId.HasValue && playerMap.TryGetValue(playerId.Value, out var player))
                    await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", seat, player.PlayerId, player.Name, player.PictureUrl ?? "user.png");
                else
                    await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", seat, 0, "Waiting", "user.png");
            }
        }
    }
}