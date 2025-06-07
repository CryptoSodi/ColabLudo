using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Newtonsoft.Json;
using SharedCode;
using SharedCode.CoreEngine;
using System.Linq;

namespace SignalR.Server
{
    public class GameRoom
    {
        public List<ChatMessages> chatMessages = new List<ChatMessages>();
        // A simple persistent store for commands.
        // In production, this might be a database or distributed log.
        private readonly List<GameCommand> _commandStore = new List<GameCommand>();
        private readonly object _commandStoreLock = new object();
        SharedCode.GameDto gameDTO;
        public List<User> Users { get; set; }
        public List<SharedCode.PlayerDto> seats = new List<SharedCode.PlayerDto>();
        public Engine engine { get; set; }  // The Engine instance for this room

        private readonly IDbContextFactory<LudoDbContext> _contextFactory;
        private readonly IHubContext<LudoHub> _hubContext;
        private readonly CryptoHelper _crypto;

        public GameRoom(IHubContext<LudoHub> hubContext, IDbContextFactory<LudoDbContext> contextFactory, CryptoHelper crypto, SharedCode.GameDto gameDTO)
            //string roomCode, string gameType, decimal gameCost)
        {
            this.gameDTO = gameDTO;
            _hubContext = hubContext;
            _contextFactory = contextFactory;
            _crypto = crypto;        
            Users = new List<User>();
        }
        public delegate Task<string> TimerTimeoutHandler(string SeatName);
        public event TimerTimeoutHandler TimerTimeout;
        private CancellationTokenSource _animationCancellationTokenSource;
        // You might include a method to initialize the Engine when the game is ready.
        public void InitializeEngine(string initialPlayerColor)
        {
            // For example, using GameType and number of users (or connection count)
            engine = new Engine("Server", gameDTO.GameType, Users.Count.ToString(), initialPlayerColor);
            engine.ShowResults += new Engine.CallbackEventHandlerShowResults(ShowResults);

            engine.StartProgressAnimation += StartProgressAnimation;
            engine.StopProgressAnimation += StopProgressAnimation;
            TimerTimeout += engine.TimerTimeoutAsync;
            
            StartProgressAnimation(engine.EngineHelper.currentPlayer.Color);
            //engine.TimerTimeoutAsync(engine.EngineHelper.currentPlayer.Color);
        }
        public Task<List<GameCommand>> PullCommands(int lastSeenIndexServer)
        {
            List<GameCommand> newCommands;
            lock (_commandStoreLock)
            {
                // Return only commands that have not been seen based on IndexServer
                newCommands = _commandStore
                    .Where(cmd => cmd.IndexServer > lastSeenIndexServer)
                    .OrderBy(cmd => cmd.IndexServer)
                    .ToList();
            }
            return Task.FromResult(newCommands);
        }
        private async Task ShowResults(string PlayerColor, string NOTUSEDGameType, string NOTUSEDGameCost)//These two are just veriation and not used 
        {
            using var context = _contextFactory.CreateDbContext();
            // Assume 'seats' is a List<Seat> and Seat has a property 'SeatColor'
            // Order the list so that seats whose SeatColor equals the provided seatColor come first.
            List<SharedCode.PlayerDto> orderedSeats;

            List<string> winnerIds = gameDTO.GameType == "22"
                    ? PlayerColor.Split(",").Select(c => c.Trim()).ToList()
                    : new List<string> { PlayerColor.Split(",")[0].Trim() };

            orderedSeats = seats.OrderByDescending(seat => winnerIds.Contains(seat.PlayerColor, StringComparer.OrdinalIgnoreCase)).ToList();

            try
            {
                // 1) Update player statistics in the DB
                UpdatePlayerStats(context, orderedSeats, winnerIds);
            }
            catch { }
            // 2) Update game state in DM and database
            var existingGame = LudoHub.DM.games.FirstOrDefault(g => g.RoomCode == gameDTO.RoomCode);
            if (existingGame != null)
            {
                existingGame.Winner1 = winnerIds[0];
                if (winnerIds.Count > 1)
                {
                    existingGame.Winner2 = winnerIds[1];
                }
                existingGame.State = "Completed";
                context.Games.Update(existingGame);
            }
            // Commit all EF changes
            await context.SaveChangesAsync();
            LudoHub.DM.SaveData();

            // After EF commit, perform SOL transfers in saga-like flow
            List<string> loserids = orderedSeats
                .Where(seat => !winnerIds.Contains(seat.PlayerColor, StringComparer.OrdinalIgnoreCase))
                .Select(s => s.PlayerId.ToString())
                .ToList();

            // Sort the seats based on the winner and losers
            for (int i = 0; i < loserids.Count && gameDTO.BetAmount > 0; i++)
            {
                var loserId = loserids[i];
                var winnerId = winnerIds.Count == 1 ? winnerIds[0] : winnerIds[i];

                try
                {
                    // Retrieve wallets and check balance
                    var loserAddress = await _crypto.GetOrCreateDepositAccountAsync(loserId);
                    var balance = await _crypto.GetSolBalanceAsync(loserAddress);
                    var lamports = (ulong)(gameDTO.BetAmount * 1_000_000_000);

                    if (balance >= lamports)
                    {
                        // Transfer SOL
                        string winnerAddress = await _crypto.GetOrCreateDepositAccountAsync(winnerId);
                        // Transfer the SOL
                        string signature = await _crypto.SendSolAsync(loserId, winnerAddress, Double.Parse(gameDTO.BetAmount + ""));
                        Console.WriteLine($"Transferred {gameDTO.BetAmount} SOL from {loserId} X {loserAddress} to {winnerId} X {winnerAddress}. Tx: {signature}");
                    }
                    else
                    {
                        Console.WriteLine($"Insufficient balance for player {loserId} X {loserAddress}. Skipping transfer.");
                    }
                }
                catch (Exception ex)
                {
                    // Log and optionally compensate later
                    Console.WriteLine($"Failed to transfer SOL from {loserId} to {winnerId}: {ex.Message}");
                }
            }

            // Instead of Thread.Sleep, use Task.Delay for async waiting.
            await Task.Delay(500);
            // Send the rearranged list to your clients (make sure your client is set up to handle this list)
            await _hubContext.Clients.Group(gameDTO.RoomCode).SendAsync("ShowResults", JsonConvert.SerializeObject(orderedSeats), gameDTO.GameType, gameDTO.BetAmount.ToString());
        }

        private void UpdatePlayerStats(LudoDbContext context,List<SharedCode.PlayerDto> orderedSeats,List<string> winnerIds)
        {
            foreach (var seat in orderedSeats)
            {
                var player = context.Players.First(p => p.PlayerId == seat.PlayerId);
                var isWinner = winnerIds.Contains(seat.PlayerColor, StringComparer.OrdinalIgnoreCase);

                if (isWinner)
                {
                    // Winner: increment win counters and update scores
                    player.GamesWon++;
                    player.TotalWin += gameDTO.BetAmount;
                    player.BestWin = Math.Max(player.BestWin, gameDTO.BetAmount);
                }
                else
                {
                    // Loser: increment loss counters and track total losses
                    player.GamesLost++;
                    player.TotalLost += gameDTO.BetAmount;
                }

                // Common updates for both winners and losers
                player.Score += engine.EngineHelper.getPlayer(seat.PlayerColor.ToLower()).Score;
                player.GamesPlayed++;
                context.Players.Update(player);
            }
        }


        public async Task<User> PlayerLeft(string connectionId,string roomCode)
        {
            // Try to find the user in the game room's user list using the connection ID.
            var user = Users.FirstOrDefault(u => u.ConnectionId == connectionId);
            if (user != null && engine != null)
            {
                // Remove the user from the room.
                Users.Remove(user);

                engine.PlayerLeft(user.PlayerColor);
                
                GameCommand command = new GameCommand
                {
                    SendToClientFunctionName = "PlayerLeft",
                    seatName = user.PlayerColor,
                    Index = engine.EngineHelper.index++
                };
                lock (_commandStoreLock)
                    _commandStore.Add(command);

                Console.WriteLine("User removed: " + user.PlayerColor);
            }
            else
            {
                Console.WriteLine("User not found for connection: " + connectionId);
            }
            return user;
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
            if (engine.EngineHelper.stopAnimate)
            {
                await Task.Delay(200);
             //   result = await TimerTimeout?.Invoke(engine.EngineHelper.currentPlayer.Color);
                Console.WriteLine($"TIMEOUT : {result}");
                return;
            }

            try
            {
                for (int i = 0; i < steps; i++)
                {
                    // Check if cancellation has been requested
                    if (token.IsCancellationRequested)
                        return;
                    if (i > 50 && engine.EngineHelper.animationBlock)
                        break;
                    await Task.Delay((int)interval);
                }
            }
            catch (Exception)
            {

            }
           // result = await TimerTimeout?.Invoke(engine.EngineHelper.currentPlayer.Color);
            Console.WriteLine($"TIMEOUT : {result}");
        }
        internal async Task<GameCommand> MovePieceAsync(GameCommand commandValue)
        {
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

                lock (_commandStoreLock)
                    _commandStore.Add(command);

                return command;
            }
            return null;
        }
        internal async Task<GameCommand> SeatTurn(GameCommand commandValue)
        {   
            if (engine.EngineHelper.checkTurn(commandValue.seatName, "RollDice"))
            {
                String result = await engine.SeatTurn(commandValue.seatName, commandValue.diceValue, commandValue.piece1, commandValue.piece2);
                Console.WriteLine($"Local : {result}");

                GameCommand command = new GameCommand
                {
                    SendToClientFunctionName = "DiceRoll",
                    seatName = commandValue.seatName,
                    diceValue = result.Split(",")[0],
                    piece1 = result.Split(",")[1],
                    piece2 = result.Split(",")[2],
                    Index = commandValue.Index,
                    IndexServer = ++engine.EngineHelper.index
                };
                lock (_commandStoreLock)
                    _commandStore.Add(command);
                return command;
            }
            return null;
        }
    }
}