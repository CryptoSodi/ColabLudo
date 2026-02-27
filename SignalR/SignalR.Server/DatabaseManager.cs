using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SignalR.Server.Services;
using System.Collections.Concurrent;

namespace SignalR.Server
{
    public class DatabaseManager(IHubContext<LudoHub> _hubContext, IDbContextFactory<LudoDbContext> _contextFactory, CryptoHelper _crypto, UtilService _utilService)
    {
        public List<Game> games { get; set; } = new();
        private List<MultiPlayer> multiPlayers { get; set; } = new();
        public ConcurrentDictionary<string, GameRoom> _gameRooms { get; set; } = new();
        
        public async Task<Game> JoinGameLobby(Player player, SharedCode.GameDto gameDTO)
        {
            Console.WriteLine("Join "+DateTime.UtcNow);
            Game existingGame = null;
            int tournamentId = -1;
            using var ctx = _contextFactory.CreateDbContext();

            if (gameDTO.IsTournamentGame)
            {
                if (!int.TryParse(gameDTO.RoomCode, out int parsedId))
                    throw new ArgumentException("Invalid tournament ID format in RoomCode.");
                tournamentId = parsedId;

                var tournament = await ctx.Tournaments.FirstOrDefaultAsync(tc => tc.TournamentId == tournamentId);
                if (tournament == null)
                    throw new Exception($"Tournament {tournamentId} not found.");

                gameDTO.BetAmount = tournament.EntryFee;
                gameDTO.RoomCode = "";
                existingGame = games.FirstOrDefault(g => g.TournamentId == tournamentId && g.State == "Active");
            }
            else if (gameDTO.IsPracticeGame)
                existingGame = games.FirstOrDefault(g => g.GameType == gameDTO.GameType && g.BetAmount == 0 && g.State == "Active");
            else
                existingGame = games.FirstOrDefault(g => g.RoomCode == gameDTO.RoomCode && g.State == "Active");

            if (existingGame == null)
            {
                bool codeExists;
                do
                {
                    gameDTO.RoomCode = new Random().Next(10000000, 99999999).ToString();
                    // Check in-memory (games and _gameRooms)
                    // Check in database
                    codeExists = games.Any(g => g.RoomCode == gameDTO.RoomCode) || _gameRooms.ContainsKey(gameDTO.RoomCode) || await ctx.Games.AnyAsync(g => g.RoomCode == gameDTO.RoomCode);
                } while (codeExists);

                // Deduct game fee
                if (!gameDTO.IsPracticeGame)
                {
                    if (!_crypto.deductGameFee(player.PlayerId, tournamentId, gameDTO.RoomCode, gameDTO.IsTournamentGame, gameDTO.BetAmount))
                    {
                        Console.WriteLine($"Game fee FAILED TO deduct for player {player.PlayerId} in room {gameDTO.RoomCode}.");
                        return null;
                    }
                }

                _gameRooms.TryAdd(gameDTO.RoomCode, new GameRoom(_hubContext, _contextFactory, this, _crypto, _utilService, gameDTO));

                MultiPlayer multiPlayer = GetGamePlayers(player.PlayerId, null);
                multiPlayer.RoomCode = int.Parse(gameDTO.RoomCode);

                existingGame = new Game
                {
                    MultiPlayerId = multiPlayer.MultiPlayerId,
                    PlayerCount = gameDTO.PlayerCount,
                    GameType = gameDTO.GameType,
                    BetAmount = gameDTO.BetAmount,
                    RoomCode = gameDTO.RoomCode,
                    IsPrivate = gameDTO.IsPrivateGame,
                    IsPractice = gameDTO.IsPracticeGame,
                    TournamentId = gameDTO.IsTournamentGame ? tournamentId : null,
                    Owner = player.PlayerId,
                    State = "Active",
                    MultiPlayer = multiPlayer,
                    IsNew = true
                };

                lock (games) // Lock the games list while modifying
                    games.Add(existingGame);
            }
            else
            {
                if (!existingGame.IsPractice)
                    if (!_crypto.deductGameFee(player.PlayerId, existingGame.TournamentId, existingGame.RoomCode, gameDTO.IsTournamentGame, existingGame.BetAmount))
                    {
                        Console.WriteLine($"Game fee FAILED TO deduct for player {player.PlayerId} in room {gameDTO.RoomCode}.");
                        return null;
                    }
                // Join existing game
                // Safely assign player slots here
                lock (existingGame)
                    existingGame.MultiPlayer = GetGamePlayers(player.PlayerId, existingGame.MultiPlayer);
                existingGame.IsDirty = true;
            }

            // Add to GameRoom
            GameRoom gameRoom = _gameRooms.GetOrAdd(existingGame.RoomCode, _ => new GameRoom(_hubContext, _contextFactory, this, _crypto, _utilService, gameDTO));
            // Add user to active users
            
            lock (gameRoom.Users) // Lock GameRoom users list
            {
                gameRoom.Users.Add(new User(player, "Color"));
            }

            await SaveData(); // Save asynchronously
            return existingGame;
        }
        public async Task<(Game game, User user)> LeaveGameLobby(int playerId)
        {
            User user = null;
            Game existingGame = null;
            try
            {
                // Find the game where this player exists
                existingGame = GetActiveGame("Active", playerId);
                if (existingGame != null)
                {
                    lock (existingGame) // Lock to prevent race conditions
                    {
                        var multiPlayer = existingGame.MultiPlayer;
                        // Clear the player slot
                        if (multiPlayer.P1 == playerId)
                            multiPlayer.P1 = null;
                        else if (multiPlayer.P2 == playerId)
                            multiPlayer.P2 = null;
                        else if (multiPlayer.P3 == playerId)
                            multiPlayer.P3 = null;
                        else if (multiPlayer.P4 == playerId)
                            multiPlayer.P4 = null;
                        multiPlayer.IsDirty = true;

                        // Check if all player slots are empty                    
                        if (multiPlayer.P1 == null && multiPlayer.P2 == null && multiPlayer.P3 == null && multiPlayer.P4 == null)
                        {
                            existingGame.State = "Terminated";
                            existingGame.IsDirty = true;
                            // Remove GameRoom from memory
                            if (_gameRooms.TryRemove(existingGame.RoomCode, out var removedRoom))
                            {
                                Console.WriteLine($"GameRoom {existingGame.RoomCode} removed from memory.");
                            }
                        }
                    }
                    // Refund the player if this is not a practice or tournament game
                    if (!existingGame.IsPractice && existingGame.TournamentId == null)
                    {
                        try
                        {
                            decimal betAmount = existingGame.BetAmount;
                            _crypto.OffChainTransaction(playerId, betAmount, "Game Refund", "", false, existingGame.RoomCode);
                            Console.WriteLine($"Refunded {betAmount} to player {playerId} for game {existingGame.RoomCode}.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Refund failed for player {playerId} in game {existingGame.RoomCode}: {ex.Message}");
                            // Optionally: Re-add the player to the game or log for manual investigation
                        }
                    }
                }
            }
            catch (Exception)
            {//If the Game is not Active it must be in playing mode then we need to broadcast to the game room that the player has left
                try
                {
                    // Find the game where this player exists
                    existingGame = existingGame ?? GetActiveGame("Playing", playerId);
                }
                catch (Exception) 
                {
                    Console.WriteLine($"No Playing game found for player {playerId}.");
                }
               
            }
            if (existingGame != null)
                {
                    // Notify GameRoom about the player leaving
                    if (_gameRooms.TryGetValue(existingGame.RoomCode, out GameRoom gameRoom))
                        user = await gameRoom.PlayerLeft(playerId);
                }

            // Persist the updated state
            try
            {
                await SaveData();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while saving game state: {ex.Message}");
            }
            return (existingGame, user);
        }

        internal async Task<(Game existingGame, List<SharedCode.PlayerDto> seats, String rollsString)> Ready(Game existingGame)
        {
            // Usage:
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                // Build playerId-to-color mapping
                var playerSlots = new (int? PlayerId, string Color)[]{
                (existingGame.MultiPlayer.P1, "Red"),
                (existingGame.MultiPlayer.P2, existingGame.GameType == "2" ? "Yellow" : "Green"),
                (existingGame.MultiPlayer.P3, "Yellow"),
                (existingGame.MultiPlayer.P4, "Blue")};
                // Get all player IDs that are not null
                var playerIds = playerSlots.Where(slot => slot.PlayerId.HasValue).Select(slot => slot.PlayerId.Value).ToList();
                // Fetch all players in a single query
                var players = await ctx.Players.Where(p => playerIds.Contains(p.PlayerId)).ToListAsync();

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
                    existingGame.IsDirty = true;
                    await SaveData();

                    await Task.Delay(2000);

                    _gameRooms.TryGetValue(existingGame.RoomCode, out GameRoom gameRoom);

                    for (int i = 0; i < gameRoom.Users.Count; i++)
                    {
                        gameRoom.Users[i].PlayerColor = seats[i].PlayerColor.ToLower();
                        gameRoom.Users[i].player = playerList[i];
                    }
                    gameRoom.InitializeEngine(seats);
                    return (existingGame, seats, gameRoom.engine.EngineHelper.rollsString);
                }
                return (existingGame, seats, "");
            }
            catch (InvalidOperationException ex)
            {
                //"No active game found."
                throw new Exception($"No active game found for player. {ex.Message}");
            }
            return (null,null,null);
        }

        private MultiPlayer GetGamePlayers(int playerId, MultiPlayer multiPlayer)
        {
            if (multiPlayer == null)
            {
                multiPlayer = new MultiPlayer{P1 = playerId, IsNew = true };
                // Add the MultiPlayer and save changes to get the MultiPlayerId
                multiPlayers.Add(multiPlayer);//_context.MultiPlayers
                //await _context.SaveChangesAsync(); // This will save the newly added MultiPlayer and assign it an Id
                return multiPlayer;
            }
            else
            {
                if (multiPlayer.P1 == null) {multiPlayer.P1 = playerId; multiPlayer.IsDirty = true; }
                else if (multiPlayer.P2 == null) { multiPlayer.P2 = playerId; multiPlayer.IsDirty = true; }
                else if (multiPlayer.P3 == null) { multiPlayer.P3 = playerId; multiPlayer.IsDirty = true; }
                else if (multiPlayer.P4 == null) { multiPlayer.P4 = playerId; multiPlayer.IsDirty = true; }
                else return null;
                return multiPlayer;
            }
        }
        public async Task SaveData()
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();

                foreach (var multiPlayer in multiPlayers)
                {
                    if (multiPlayer.IsNew)
                        ctx.MultiPlayers.Add(multiPlayer);
                    else if (multiPlayer.IsDirty)
                        ctx.MultiPlayers.Update(multiPlayer);

                    // Reset flags
                    multiPlayer.IsNew = false;
                    multiPlayer.IsDirty = false;
                }

                foreach (var game in games)
                {
                    if (game.IsNew)
                        ctx.Games.Add(game);
                    else if (game.IsDirty)
                        ctx.Games.Update(game);
                    // Reset flags
                    game.IsNew = false;
                    game.IsDirty = false;
                    Console.WriteLine($"Saving game {game.RoomCode} with state {game.State} and players: {game.MultiPlayer.P1}, {game.MultiPlayer.P2}, {game.MultiPlayer.P3}, {game.MultiPlayer.P4}");
                }

                await ctx.SaveChangesAsync();
                Console.WriteLine("Data saved successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving data: {ex.Message}");
            }
        }
        public async Task LoadData()
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();
                games = await ctx.Games.Include(g => g.MultiPlayer).Where(g=>g.State == "Active").ToListAsync();
               // multiPlayers = new List<MultiPlayer>(); await ctx.MultiPlayers.ToListAsync();

                foreach (var game in games)
                {
                    if (game.MultiPlayer != null && !multiPlayers.Any(mp => mp.MultiPlayerId == game.MultiPlayer.MultiPlayerId))
                        multiPlayers.Add(game.MultiPlayer);// = multiPlayers.FirstOrDefault(mp => mp.MultiPlayerId == game.MultiPlayerId);                    
                    SharedCode.GameDto gameDto = new SharedCode.GameDto
                    {
                        GameType = game.GameType,
                        RoomCode = game.RoomCode,
                        BetAmount = game.BetAmount,
                        PlayerCount = game.PlayerCount,
                        IsPracticeGame = game.BetAmount == 0,
                        IsTournamentGame = game.TournamentId != null,
                        playerColor = "DefaultColor" // Set a default color or retrieve from the database if needed
                    };

                    _gameRooms.TryAdd(game.RoomCode, new GameRoom(_hubContext, _contextFactory, this, _crypto, _utilService, gameDto));
                }

                Console.WriteLine("Data loaded successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data: {ex.Message}");
            }
        }
        internal Game GetActiveGame(String State, int playerId)
        {
            //State == Active mostly but we can get Playing games too
            Game game = games.FirstOrDefault(g => g.State == State &&
                (g.MultiPlayer.P1 == playerId ||
                 g.MultiPlayer.P2 == playerId ||
                 g.MultiPlayer.P3 == playerId ||
                 g.MultiPlayer.P4 == playerId));
            if (game == null)
                throw new InvalidOperationException("No active game found.");
            return game;
        }
    }
}