using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SharedCode;
using SharedCode.CoreEngine;
using SignalR.Server.Payments;
using SignalR.Server.Services;

namespace SignalR.Server
{
    public class GameRoom(IDbContextFactory<LudoDbContext> _contextFactory, DatabaseManager DM, CryptoHelper _crypto, UtilService _utilService, SharedCode.GameDto gameDTO)
    {
        public SharedCode.GameDto gameDTO { get; } = gameDTO;
        private readonly SemaphoreSlim _roomLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource? _animationCancellationTokenSource;
        public Engine? engine { get; set; }  // The Engine instance for this room
        public List<User> Users { get; set; } = new List<User>();
        public List<SharedCode.PlayerDto> _seats { get; set; }
        private List<GameCommand> _commandStore { get; set; } = new List<GameCommand>();
        public List<ChatMessages> chatMessages { get; set; } = new List<ChatMessages>();        
        // You might include a method to initialize the Engine when the game is ready.
        public void InitializeEngine(List<SharedCode.PlayerDto> seats)
        {
            _seats = seats;
            // For example, using GameType and number of users (or connection count)
            engine = new Engine("Server", gameDTO.GameType, Users.Count.ToString(), seats[0].PlayerColor);
            engine.ShowResults += new Engine.CallbackEventHandlerShowResults(ShowResults);

            engine.StartProgressAnimation += StartProgressAnimation;
            engine.StopProgressAnimation += StopProgressAnimation;            

            StartProgressAnimation(engine.EngineHelper.currentPlayer.Color);
            //engine.TimerTimeoutAsync(engine.EngineHelper.currentPlayer.Color);
        }
        public Task<List<GameCommand>> PullCommands(int lastSeenIndexServer)
        {
            lock (_commandStore)// Return only commands that have not been seen based on IndexServer
                return Task.FromResult(_commandStore.Where(cmd => cmd.IndexServer > lastSeenIndexServer).OrderBy(cmd => cmd.IndexServer).ToList());
        }
        private async Task ShowResults(string PlayerColor, string NOTUSEDGameType, string NOTUSEDGameCost)//These two are just veriation and not used 
        {
            using var ctx = _contextFactory.CreateDbContext();
            // Assume 'seats' is a List<Seat> and Seat has a property 'SeatColor'
            // Order the list so that seats whose SeatColor equals the provided seatColor come first.
            List<SharedCode.PlayerDto> orderedSeats;
            // 2) Update game state in DM and database
            var existingGame = ctx.Games.Include(g => g.MultiPlayer).FirstOrDefault(g => g.RoomCode == gameDTO.RoomCode);            

            List<string> winnerIds = gameDTO.GameType == "22"
                    ? PlayerColor.Split(",").Select(c => c.Trim()).ToList()
                    : new List<string> { PlayerColor.Split(",")[0].Trim() };

            orderedSeats = _seats.OrderByDescending(seat => winnerIds.Contains(seat.PlayerColor, StringComparer.OrdinalIgnoreCase)).ToList();
            // After EF commit, perform SOL transfers in saga-like flow
            //List<int> loserids = orderedSeats
            //    .Where(seat => !winnerIds.Contains(seat.PlayerColor, StringComparer.OrdinalIgnoreCase))
            //    .Select(s => s.PlayerId).ToList();

            // 1) Update player statistics in the DB
            UpdatePlayerStats(orderedSeats, winnerIds , existingGame.BetAmount);

            if (existingGame != null)
            {
                existingGame.Winner1 = orderedSeats.FirstOrDefault(seat =>string.Equals(seat.PlayerColor, winnerIds[0], StringComparison.OrdinalIgnoreCase)).PlayerId;
                if (winnerIds.Count > 1)
                    existingGame.Winner2 = orderedSeats.FirstOrDefault(seat => string.Equals(seat.PlayerColor, winnerIds[1], StringComparison.OrdinalIgnoreCase)).PlayerId;

                existingGame.State = "Completed";
                ctx.Games.Update(existingGame);
                ctx.SaveChanges();
            }
    
            decimal betAmount = existingGame.BetAmount; // per player
            decimal totalPot = betAmount * orderedSeats.Count;
            Console.WriteLine($"Total Pot: {totalPot} SOL");
            // Distribute the pot among winners
            decimal winningsPerWinner = winnerIds.Count > 0 ? totalPot / winnerIds.Count : 0;

            // Credit each winner **once**
            foreach (var winnerColor in winnerIds)
            {
                var winnerSeat = orderedSeats.FirstOrDefault(seat => string.Equals(seat.PlayerColor, winnerColor, StringComparison.OrdinalIgnoreCase));
                
                if (winnerSeat == null) continue; // Defensive: in case of missing mapping
                var winnerId = winnerSeat.PlayerId;
                try
                {
                    // 💰 FIX: Pass existingGame.RoomCode and TransactionType.GameWin
                    bool credited = await _crypto.OffChainTransaction(winnerId, winningsPerWinner, "Game Won", "", false, existingGame.RoomCode, TransactionType.GameWin);
                    if (!credited)
                    {
                        Console.WriteLine($"Failed to credit {winnerId}.");
                        // Optionally add to compensation queue
                        continue;
                    }
                    else
                    {
                        try
                        {
                            await Task.CompletedTask;
                        }
                        catch (Exception)
                        {
                        }
                    }
                    Console.WriteLine($"Off-chain transferred {winningsPerWinner} SOL to {winnerId}.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error crediting {winnerId}: {ex.Message}");
                    // Optionally add to compensation queue
                }
            }
            StopProgressAnimation("");
            // Instead of Thread.Sleep, use Task.Delay for async waiting.
            await Task.Delay(500);
            // Queue ShowResults in pull-command stream so HTTP polling clients receive it.
            var showResultsCommand = new GameCommand
            {
                SendToClientFunctionName = "ShowResults",
                ShowResultsSeats = JsonConvert.SerializeObject(orderedSeats),
                ShowResultsGameType = gameDTO.GameType,
                ShowResultsGameCost = gameDTO.BetAmount.ToString(),
                Index = _commandStore.Count,
                IndexServer = ++engine.EngineHelper.index
            };

            lock (_commandStore)
                _commandStore.Add(showResultsCommand);

            await Task.CompletedTask;
        }
        private void UpdatePlayerStats(List<SharedCode.PlayerDto> orderedSeats, List<string> winnerIds, decimal BetAmount)
        {
            using var ctx = _contextFactory.CreateDbContext();
            var existingGame = ctx.Games.FirstOrDefault(g => g.RoomCode == gameDTO.RoomCode);

            foreach (var seat in orderedSeats)
            {
                var player = ctx.Players.FirstOrDefault(p => p.PlayerId == seat.PlayerId);
                var isWinner = winnerIds.Contains(seat.PlayerColor, StringComparer.OrdinalIgnoreCase);

                if (isWinner)
                {
                    player.GamesWon++;
                    player.TotalWin += BetAmount;
                    player.BestWin = Math.Max(player.BestWin, BetAmount);

                    // If this is a tournament game, increment the winner's tournament score (wins count)
                    if (existingGame != null && existingGame.TournamentId.HasValue)
                    {
                        var challenger = ctx.TournamentChallengers.FirstOrDefault(tc => 
                            tc.TournamentId == existingGame.TournamentId && tc.PlayerId == player.PlayerId);
                        
                        if (challenger != null)
                        {
                            challenger.Score++; // Score acts as the "Games Won" count for tournaments
                            ctx.TournamentChallengers.Update(challenger);
                        }
                    }
                }
                else
                {
                    player.TotalLost += BetAmount;
                    player.GamesLost++;
                }

                player.Score += engine.EngineHelper.getPlayer(seat.PlayerColor.ToLower()).Score;
                player.GamesPlayed++;
                ctx.Players.Update(player);
            }
            ctx.SaveChanges();
        }
        public async void StartProgressAnimation(string SeatName)
        {
            // Wait until the component has rendered
            // Cancel any previous animation
            StopProgressAnimation("");
            _animationCancellationTokenSource = new CancellationTokenSource();
            await AnimateProgress(_animationCancellationTokenSource.Token);
        }
        public void StopProgressAnimation(string SeatName)
        {
            if (_animationCancellationTokenSource != null)
            {
                _animationCancellationTokenSource.Cancel();
                _animationCancellationTokenSource.Dispose();
                _animationCancellationTokenSource = null;
            }
        }
        private async Task AnimateProgress(CancellationToken token)
        {
            const int duration = 10000; // Total duration in milliseconds (10 seconds)
            const int interval = 20;    // Delay interval per iteration in milliseconds
            int steps = duration / interval; // This gives 500 iterations
            string result = "";
            try
            {
                for (int i = 0; i < steps; i++)
                {
                    // Check if cancellation has been requested
                    if (token.IsCancellationRequested)
                    {
                        Console.WriteLine("TIMER Animation cancelled.");
                        return;
                    }
                    if (i > 50 && engine.EngineHelper.animationBlock)
                        break;
                    await Task.Delay((int)interval);
                }
            }
            catch (Exception) { }

            String seatName = engine.EngineHelper.currentPlayer.Color;
            if (engine.EngineHelper.checkTurn(engine.EngineHelper.currentPlayer.Color, "RollDice"))
            {
                result = await engine.SeatTurn(engine.EngineHelper.currentPlayer.Color, "", "", "");                
                GameCommand command = new GameCommand
                {
                    SendToClientFunctionName = "DiceRoll",
                    seatName = seatName,
                    diceValue = result.Split(",")[0],
                    piece1 = result.Split(",")[1],
                    piece2 = result.Split(",")[2],
                    Index = _commandStore.Count,
                    IndexServer = ++engine.EngineHelper.index
                };
                lock (_commandStore)
                    _commandStore.Add(command);
            }
            else if (engine.EngineHelper.checkTurn(engine.EngineHelper.currentPlayer.Color, "MovePiece"))
            {
                int diceValue = engine.EngineHelper.diceValue;
                
                result = engine.EngineHelper.AIRequestPiece();
                result = await engine.MovePieceAsync(result.Split(",")[0], result.Split(",")[1]);
                
                Console.WriteLine(result);
                GameCommand command = new GameCommand
                {
                    SendToClientFunctionName = "MovePiece",
                    seatName = seatName,
                    diceValue = diceValue.ToString(),
                    piece1 = result.Split(",")[0],
                    piece2 = result.Split(",")[1],
                    Index = _commandStore.Count,
                    IndexServer = ++engine.EngineHelper.index
                };
                lock (_commandStore)
                    _commandStore.Add(command);
            }
            Console.WriteLine($"TIMEOUT : {result}");
        }
        public async Task<GameCommand> MovePieceAsync(string authToken, GameCommand commandValue)
        {
            await _roomLock.WaitAsync();
            try
            {
                // Authenticate user
                var user = Users.FirstOrDefault(u => u.player.AuthToken == authToken);
                if (user == null)
                {
                    Console.WriteLine("Authentication failed: Invalid token.");
                    return null; // or throw an UnauthorizedAccessException
                }
                // Check if user's seat matches the command's seat and current player's turn
                if (user.PlayerColor != commandValue.seatName || user.PlayerColor != engine.EngineHelper.currentPlayer.Color)
                {
                    Console.WriteLine("Authorization failed: User trying to move out of turn or from wrong seat.");
                    return null; // or throw an InvalidOperationException
                }
                if (engine.EngineHelper.checkTurn(commandValue.piece1, "MovePiece"))
                {
                    String result = "FAILED";
                    result = await engine.MovePieceAsync(commandValue.piece1, commandValue.piece2);

                    GameCommand command = new GameCommand
                    {
                        SendToClientFunctionName = "MovePiece",
                        seatName = commandValue.seatName,
                        diceValue = commandValue.diceValue,
                        piece1 = result.Split(",")[0],
                        piece2 = result.Split(",")[1],
                        Index = commandValue.Index,
                        IndexServer = ++engine.EngineHelper.index
                    };

                    lock (_commandStore)
                        _commandStore.Add(command);

                    return command;
                }
                return null;
            }
            finally
            {
                _roomLock.Release();
            }
        }
        public async Task<GameCommand> SeatTurn(string authToken, GameCommand commandValue)
        {
            await _roomLock.WaitAsync();
            try
            {
                var user = Users.FirstOrDefault(u => u.player.AuthToken == authToken);
                if (user == null)
                {
                    Console.WriteLine("Authentication failed: Invalid token.");
                    return null; // or throw an UnauthorizedAccessException
                }
                // Check if user's seat matches the command's seat and current player's turn
                if (user.PlayerColor != commandValue.seatName || user.PlayerColor != engine.EngineHelper.currentPlayer.Color)
                {
                    Console.WriteLine("Authorization failed: User trying to move out of turn or from wrong seat.");
                    return null; // or throw an InvalidOperationException
                }
                if (engine.EngineHelper.checkTurn(commandValue.seatName, "RollDice"))
                {
                    String result = await engine.SeatTurn(commandValue.seatName, commandValue.diceValue, commandValue.piece1, commandValue.piece2);
                    if (result.Contains("-1") || result.Contains("-0"))
                    {
                        //FAILED, likely due to invalid move. Don't increment index or add to command store.
                    }
                    else
                    {
                        Console.WriteLine($"Local : {result}");
                        GameCommand command = new GameCommand
                        {
                            SendToClientFunctionName = "DiceRoll",
                            seatName = commandValue.seatName,
                            diceValue = result.Split(",")[0],
                            piece1 = result.Split(",")[1],
                            piece2 = result.Split(",")[2],
                            Index = commandValue.Index,
                            IndexServer = ++engine.EngineHelper.index,
                            Result = "Success"
                        };
                        lock (_commandStore)
                            _commandStore.Add(command);
                        return command;
                    }
                }
                return null;
            }
            finally
            {
                _roomLock.Release();
            }
        }
        public async Task<User> PlayerLeft(int playerId)
        {
            await _roomLock.WaitAsync();
            try
            {
                // Try to find the user in the game room's user list using the connection ID.
                var user = Users.FirstOrDefault(u => u.player?.PlayerId == playerId);
                if (user != null)
                {
                    // Remove the user from the room.
                    Users.Remove(user);
                    if (engine != null)
                    {
                        await engine.PlayerLeft(user.PlayerColor);
                        GameCommand command = new GameCommand
                        {
                            SendToClientFunctionName = "PlayerLeft",
                            seatName = user.PlayerColor,
                            Index = engine.EngineHelper.index
                        };
                        lock (_commandStore)
                            _commandStore.Add(command);
                    }
                    Console.WriteLine("User removed: " + user.PlayerColor);
                }
                else
                {
                    Console.WriteLine("User not found for connection: " + playerId);
                }
                return user;
            }
            finally
            {
                _roomLock.Release();
            }
        }
    }
}
public class User(LudoServer.Models.Player player, string playerColor)
{
    public LudoServer.Models.Player player { get; set; } = player;
    public string PlayerColor { get; set; } = playerColor;
}
