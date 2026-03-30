using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SharedCode;
using SharedCode.Constants;
using SignalR.Server.Payments;
using SignalR.Server.Services;
using System.Collections.Concurrent;

namespace SignalR.Server
{// A simple command class that holds details for a command.

    public class LudoHub(IDbContextFactory<LudoDbContext> _contextFactory, DatabaseManager DM, CryptoHelper _crypto, FriendsService _friendsService, TournamentService _tournamentService, DailyBonusService _dailyBonusService, GoogleAuthService _googleAuthService, UtilService _utilService) : Hub
    {
        // Thread-safe connection mappings        
        public static ConcurrentDictionary<string, Player> ConnectionToPlayer = new ConcurrentDictionary<string, Player>();
        public async Task<PlayerInfo> GoogleAuthentication(string idToken, string city, string countryCode)
        {
            try
            {
                var player = await _googleAuthService.GoogleAuthentication(idToken, city, countryCode);
                if (player == null) return null;

                // 🛑 Access Block Check
                if (player.IsBlocked)
                {
                    throw new HubException("ACCOUNT_BLOCKED");
                }

                ConnectionToPlayer[Context.ConnectionId]  = await _utilService.GetPlayerByID(player.PlayerId);
                await _utilService.SetPlayerOnlineState(player.PlayerId, true);
                
                PlayerInfo playerInfo = await _utilService.CastPlayerToInfoAsync(player);
                
                return playerInfo;
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
                var player = await _utilService.GetPlayerByID(int.Parse(_utilService.Decrypt(AuthToken)));
                
                // 🛑 Access Block Check
                if (player != null && player.IsBlocked)
                {
                    throw new HubException("ACCOUNT_BLOCKED");
                }

                ConnectionToPlayer[Context.ConnectionId] = player;
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
            try
            {
                Console.WriteLine($"User connected: {Context.ConnectionId}");
                if (ConnectionToPlayer.TryGetValue(Context.ConnectionId, out var playerAtConnection))
                    await _utilService.SetPlayerOnlineState(playerAtConnection.PlayerId, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnConnectedAsync: {ex.Message}");
            }
            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                Console.WriteLine($"User Disconnected: {Context.ConnectionId}");
                await LeaveCloseLobby();
                if (ConnectionToPlayer.TryRemove(Context.ConnectionId, out var playerAtConnection))
                {
                    await _utilService.SetPlayerOnlineState(playerAtConnection.PlayerId, false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnDisconnectedAsync: {ex.Message}");
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

                // 🛑 Access Block Check
                using var ctx = _contextFactory.CreateDbContext();
                var dbPlayer = await ctx.Players.AsNoTracking().FirstOrDefaultAsync(p => p.PlayerId == player.PlayerId);
                if (dbPlayer != null && dbPlayer.IsBlocked)
                {
                    throw new HubException("ACCOUNT_BLOCKED");
                }

                return player;
            }
            throw new HubException("Player not recognized.");
        }
        public async Task<String> Withdraw(string destination, decimal amountInSol)
        {
            try
            {
                Player player = await GetCallerPlayer();
                var r = _crypto.Withdraw(player, destination, amountInSol);
                await Clients.Caller.SendAsync("PlayerInfoUpdate", await _utilService.CastPlayerToInfoAsync(player));
                return r;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return ex.Message;
            }
        }
        public Task<List<GameCommand>> PullCommands(int lastSeenIndex, String RoomCode)
        {
            try
            {
                if (!DM._gameRooms.TryGetValue(RoomCode, out GameRoom gameRoom))
                {
                    Console.WriteLine($"GameRoom not found for room: {RoomCode}");
                    return Task.FromResult(new List<GameCommand>());
                }
                return gameRoom.PullCommands(lastSeenIndex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PullCommands: {ex.Message}");
                return Task.FromResult(new List<GameCommand>());
            }
        }
        public async Task Ping()
        {
            try
            {
                if (ConnectionToPlayer.TryGetValue(Context.ConnectionId, out var player))
                {
                    player.LastPingUtc = DateTime.UtcNow;
                }
            }
            catch { }
        }
        public async Task LeaveCloseLobby()
        {
            try
            {
                Player player = await GetCallerPlayer();
                var (existingGame, user) = await DM.LeaveGameLobby(player.PlayerId);
                // Optionally, perform additional cleanup or update the game engine state.                
                // Notify all connected clients that a user has left.
                await BroadcastPlayersAsync(existingGame);
                await Clients.Caller.SendAsync("PlayerInfoUpdate", await _utilService.CastPlayerToInfoAsync(player));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LeaveCloseLobby: {ex.Message}");
                return; // Exit if player retrieval fails
            }
        }
        public async Task<string> Ready()
        {
            try
            {
                Console.WriteLine("Ready " + DateTime.UtcNow);
                Player player = await GetCallerPlayer();
                var (existingGameReady, seats, rollsString) = await DM.Ready(player.PlayerId);
                await BroadcastPlayersAsync(existingGameReady);
                if (existingGameReady != null && seats != null && rollsString != "")
                {
                    await Clients.Group(existingGameReady.RoomCode).SendAsync("GameStarted", existingGameReady.GameType, JsonConvert.SerializeObject(seats), rollsString);
                    Console.WriteLine($"game started {DateTime.UtcNow} : {existingGameReady.RoomCode}");
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
            GameCommand Result = new GameCommand();
            try
            {
                Player player = await GetCallerPlayer();
                // For logging purposes, show which room this command is coming from.
                Console.WriteLine($"{player.Name} (room {roomCode}): {commandValue}:{commandtype}"); 
                if (player.AuthToken != AuthToken)
                {
                    Result.Result = "Error: Invalid AuthToken.";
                    return Result;
                }
                // Now use the user's Room property to get the GameRoom.
                if (!DM._gameRooms.TryGetValue(roomCode, out GameRoom gameRoom))
                {
                    Console.WriteLine($"GameRoom not found for room: {roomCode}");
                    //
                    Result.Result = "Error: Room not found.";
                    return Result;
                }
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Send: {ex.Message}");
                Result.Result = $"Error: {ex.Message}";
            }
            return Result;
        }
        /* CHAT AND FRIENDS MANAGEMENT */
        public async Task<List<ChatMessages>> SendChatMessage(ChatMessages CM, string roomCode)
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
                    CM.RoomCode = roomCode;
                    gameRoom.chatMessages.Add(CM);
                }
                List<User> otherUsers = gameRoom.Users.Where(p => p.player.PlayerId != CM.SenderId).ToList();
                User senderUser = gameRoom.Users.Where(p => p.player.PlayerId == CM.SenderId).ToList()[0];
                CM.SenderColor = senderUser.PlayerColor;

                foreach (User u in otherUsers)
                {
                    try
                    {
                        if (CM.Message != "")
                        {
                            var receiverConnections = ConnectionToPlayer.Where(kv => kv.Value.PlayerId == u.player.PlayerId).Select(kv => kv.Key).ToList();
                            // Also include sender connections to keep all sessions in sync
                            var senderConnections = ConnectionToPlayer.Where(kv => kv.Value.PlayerId == CM.SenderId).Select(kv => kv.Key).ToList();
                            
                            var allConnections = receiverConnections.Union(senderConnections).ToList();

                            foreach (var connId in allConnections)
                            {
                                await Clients.Client(connId).SendAsync("ReceiveChatMessage", new List<ChatMessages> { CM });
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                return gameRoom.chatMessages.Take(20).ToList();
            }
            else
            {
                using var ctx = _contextFactory.CreateDbContext();

                // Check for BLOCK status before proceeding
                var isBlocked = await ctx.FriendsRequests.AnyAsync(fr => 
                    ((fr.SenderId == CM.SenderId && fr.ReceiverId == CM.ReceiverId) ||
                     (fr.SenderId == CM.ReceiverId && fr.ReceiverId == CM.SenderId)) &&
                    fr.Status == "BLOCK");

                if (isBlocked)
                {
                    // SILENT BLOCK: The message is not saved and not broadcast
                    return new List<ChatMessages>();
                }

                ChatMessage? savedMessage = null;
                if (CM.Message != "")
                {
                    // 1️⃣ Save the new message to the database                    
                    savedMessage = new ChatMessage
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
                    ctx.ChatMessages.Add(savedMessage);
                    ctx.SaveChanges();
                }

                List<ChatMessage> chatHistory = ctx.ChatMessages
                    .Where(cm => (cm.SenderId == CM.SenderId && cm.ReceiverId == CM.ReceiverId) ||
                                 (cm.SenderId == CM.ReceiverId && cm.ReceiverId == CM.SenderId))
                    .OrderByDescending(cm => cm.Index)
                    .Take(30)
                    .ToList();
                
                // Sort ascending for the client
                chatHistory = chatHistory.OrderBy(cm => cm.Index).ToList();

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

                try
                {
                    var receiverConnections = ConnectionToPlayer.Where(kv => kv.Value.PlayerId == CM.ReceiverId).Select(kv => kv.Key).ToList();
                    var senderConnections = ConnectionToPlayer.Where(kv => kv.Value.PlayerId == CM.SenderId).Select(kv => kv.Key).ToList();

                    if (savedMessage != null)
                    {
                        var persistedMsg = chatMessagesList.FirstOrDefault(m => m.Index == savedMessage.Index);
                        if (persistedMsg != null)
                        {
                            // Broadcast the new message to the receiver's sessions
                            foreach (var connId in receiverConnections)
                            {
                                await Clients.Client(connId).SendAsync("ReceiveChatMessage", new List<ChatMessages> { persistedMsg });
                            }
                            
                            // Broadcast to OTHER sessions of the sender (not the caller, they get the return list)
                            foreach (var connId in senderConnections.Where(c => c != Context.ConnectionId))
                            {
                                await Clients.Client(connId).SendAsync("ReceiveChatMessage", new List<ChatMessages> { persistedMsg });
                            }
                        }
                    }
                    else if (CM.Message == "")
                    {
                        // Send the whole history back to the caller's session
                        await Clients.Caller.SendAsync("ReceiveChatMessage", chatMessagesList);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error broadcasting chat: {ex.Message}");
                }
                return chatMessagesList;
            }
        }
        /* END CHAT AND FRIENDS MANAGEMENT */
        /* DAILY BONUS */
        public async Task<DailyBonusDto> GetDailyBonus()
        {
            try
            {
                Player player = await GetCallerPlayer();
                return await _dailyBonusService.GetDailyBonus(player);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetDailyBonus: " + ex.Message);
                return null;
            }
        }
        // New function: Claim today's bonus and update LastResetDate
        public async Task<DailyBonusDto> ClaimTodayBonus()
        {
            try
            {
                Player player = await GetCallerPlayer();
                var r = await _dailyBonusService.ClaimTodayBonus(player);
                await Clients.Caller.SendAsync("PlayerInfoUpdate", await _utilService.CastPlayerToInfoAsync(player));
                return r;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in ClaimTodayBonus: " + ex.Message);
                return null;
            }
        }
        /* END DAILY BONUS */
        /* TOURNAMENT API */
        public async Task<TournamentResultDTO> GetResultsTournament(int tournamentId)
        {
            return await _tournamentService.GetResultsTournament(tournamentId);
        }
        public async Task<List<TournamentDTO>> GetAllTournaments(string type)
        {
            try
            {
                Player player = await GetCallerPlayer();
                return await _tournamentService.GetAllTournaments(player, type);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllTournaments: {ex.Message}");
                return new List<TournamentDTO>();
            }
        }
        public async Task<TournamentDTO> JoinTournament(int tournamentId)
        {
            try
            {
                Player player = await GetCallerPlayer();
                var r = await _tournamentService.JoinTournament(player, tournamentId);
                await Clients.Caller.SendAsync("PlayerInfoUpdate", await _utilService.CastPlayerToInfoAsync(player));
                return r;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in JoinTournament: {ex.Message}");
                return null;
            }
        }
        public async Task<string> MintNFT(int amount)
        {
            try
            {
                Player player = await GetCallerPlayer();
                String r = await _crypto.MintNFT(player.PlayerId ,amount);
                await Clients.Caller.SendAsync("PlayerInfoUpdate", await _utilService.CastPlayerToInfoAsync(player));
                return r;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in JoinTournament: {ex.Message}");
                return "Failed";
            }
        }
        /* END TOURNAMENT API */
        /* FRIENDS API */
        public async Task<List<PlayerCard>> GetFriends(string type = "All")
        {
            try
            {
                var player = await GetCallerPlayer(); // Assume async
                return await _friendsService.GetFriends(player, type);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetFriends: {ex.Message}");
                return null;
            }
        }
        public async Task<string> SendFriendRequest(int receiverId, string status)
        {
            try
            {
                var player = await GetCallerPlayer();
                return await _friendsService.SendFriendRequest(player, receiverId, status);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendFriendRequest: {ex.Message}");
                return "Error: " + ex.Message;
            }
        }

        public async Task<PlayerCard> GetPlayerById(int playerId)
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                var p = await ctx.Players.FirstOrDefaultAsync(x => x.PlayerId == playerId);
                if (p == null) return null;

                return new PlayerCard
                {
                    playerID = p.PlayerId,
                    name = p.Name,
                    pictureUrl = p.PictureUrl,
                    rank = ctx.Players.Count(other => other.GamesWon > p.GamesWon) + 1,
                    status = "",
                    lastGame = false,
                    gamesWon = p.GamesWon
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetPlayerById: {ex.Message}");
                return null;
            }
        }
        /* END FRIENDS API */
        public async Task<List<PlayerCard>> GetLeaderboard()
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                // Fetch ALL players with Role \"Player\" and at least 1 win, ordered by wins
                var topPlayers = await ctx.Players
                    .Where(p => p.Role == "Player" && p.GamesWon > 0)
                    .OrderByDescending(p => p.GamesWon)
                    .Select(p => new PlayerCard
                    {
                        playerID = p.PlayerId,
                        name = p.Name,
                        pictureUrl = p.PictureUrl,
                        rank = 0,
                        status = "",
                        lastGame = false,
                        gamesWon = p.GamesWon
                    })
                    .ToListAsync();

                // Assign absolute rank based on list position
                for (int i = 0; i < topPlayers.Count; i++)
                {
                    topPlayers[i].rank = i + 1;
                }

                return topPlayers;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetLeaderboard: {ex.Message}");
                return new List<PlayerCard>();
            }
        }

        public async Task<List<PlayerCard>> GetTournamentLeaderboard(string tournamentType)
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                var now = DateTime.UtcNow;

                // Find the current active tournament of this type (Daily, Weekly, etc.)
                var tournament = await ctx.Tournaments
                    .Where(t => t.Name.Contains(tournamentType) && t.TournamentState == State.Active)
                    .OrderByDescending(t => t.TournamentId)
                    .FirstOrDefaultAsync();

                if (tournament == null)
                    return new List<PlayerCard>();

                // Get all challengers for this tournament with Score > 0, ranked by Score
                var challengers = await ctx.TournamentChallengers
                    .Where(tc => tc.TournamentId == tournament.TournamentId && tc.Score > 0)
                    .Include(tc => tc.Player)
                    .OrderByDescending(tc => tc.Score)
                    .Select(tc => new PlayerCard
                    {
                        playerID = tc.PlayerId,
                        name = tc.Player.Name,
                        pictureUrl = tc.Player.PictureUrl,
                        rank = 0, // Assigned below
                        status = "",
                        lastGame = false,
                        gamesWon = tc.Score // For tournaments, we show Score in the games won column
                    })
                    .ToListAsync();

                // Group by PlayerId to ensure uniqueness if data is inconsistent
                var uniqueChallengers = challengers
                    .GroupBy(c => c.playerID)
                    .Select(g => g.First())
                    .ToList();

                for (int i = 0; i < uniqueChallengers.Count; i++)
                {
                    uniqueChallengers[i].rank = i + 1;
                }

                return uniqueChallengers;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetTournamentLeaderboard: {ex.Message}");
                return new List<PlayerCard>();
            }
        }
        public async Task<string> CreateJoinLobby(SharedCode.GameDto gameDTO)
        {
            try
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

                await Clients.Caller.SendAsync("PlayerInfoUpdate", await _utilService.CastPlayerToInfoAsync(player));
                return existingGame.RoomCode; // Return the room name to the client
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateJoinLobby: {ex.Message}");
                return "Error: " + ex.Message;
            }
        }
        /// Gets a list of all games.
        public async Task<List<Game>> GetGame(bool IsPrivate)
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                //g.State == "Active"
                return await ctx.Games.Where(g => g.State == "Active" && g.IsPrivate == IsPrivate && !g.IsPractice).ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetGame: {ex.Message}");
                return new List<Game>();
            }
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
                    await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", seat, player.PlayerId, player.Name, player.PictureUrl ?? "user.webp");
                else
                    await Clients.Group(existingGame.RoomCode).SendAsync("PlayerSeat", seat, 0, "Waiting", "user.webp");
            }
        }
    }
}