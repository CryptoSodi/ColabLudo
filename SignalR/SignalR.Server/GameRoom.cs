using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Newtonsoft.Json;
using SharedCode;
using SharedCode.CoreEngine;

namespace SignalR.Server
{
    public class GameRoom
    {
        // A simple persistent store for commands.
        // In production, this might be a database or distributed log.
        private static readonly List<GameCommand> _commandStore = new List<GameCommand>();
        private static readonly object _commandStoreLock = new object();
        public string RoomCode { get; set; }
        public string GameType { get; set; }
        public decimal GameCost { get; set; }
        public List<User> Users { get; set; }
        public List<SharedCode.PlayerDto> seats = new List<SharedCode.PlayerDto>();
        public Engine engine { get; set; }  // The Engine instance for this room

        private readonly IDbContextFactory<LudoDbContext> _contextFactory;
        private readonly IHubContext<LudoHub> _hubContext;

        public GameRoom(IHubContext<LudoHub> hubContext, IDbContextFactory<LudoDbContext> contextFactory, string roomCode, string gameType, decimal gameCost)
        {
            _hubContext = hubContext;
            _contextFactory = contextFactory;
            RoomCode = roomCode;
            GameType = gameType;
            GameCost = gameCost;
            Users = new List<User>();
        }
        public delegate Task<string> TimerTimeoutHandler(string SeatName);
        public event TimerTimeoutHandler TimerTimeout;
        private CancellationTokenSource _animationCancellationTokenSource;
        // You might include a method to initialize the Engine when the game is ready.
        public void InitializeEngine(string initialPlayerColor)
        {
            // For example, using GameType and number of users (or connection count)
            engine = new Engine("Server", GameType, Users.Count.ToString(), initialPlayerColor);
            engine.ShowResults += new Engine.CallbackEventHandlerShowResults(ShowResults);

            engine.StartProgressAnimation += StartProgressAnimation;
            engine.StopProgressAnimation += StopProgressAnimation;
            TimerTimeout += engine.TimerTimeoutAsync;
            
            StartProgressAnimation(engine.EngineHelper.currentPlayer.Color);
            //engine.TimerTimeoutAsync(engine.EngineHelper.currentPlayer.Color);
        }

        public Task<List<GameCommand>> PullCommands(int lastSeenIndex)
        {
            List<GameCommand> newCommands;
            lock (_commandStoreLock)
            {
                // Get all commands with index greater than lastSeenIndex.
                newCommands = _commandStore.Where(cmd => cmd.Index > lastSeenIndex).ToList();
            }
            return Task.FromResult(newCommands);
        }
        private async Task ShowResults(string PlayerColor, string NOTUSEDGameType, string NOTUSEDGameCost)//These two are just veriation and not used 
        {
            // Instead of Thread.Sleep, use Task.Delay for async waiting.
            await Task.Delay(2000);
            // Assume 'seats' is a List<Seat> and Seat has a property 'SeatColor'
            // Order the list so that seats whose SeatColor equals the provided seatColor come first.
            List<SharedCode.PlayerDto> sortedSeats;
            if (GameType == "22")
            {
                String winner1 = PlayerColor.Split(",")[0];
                String winner2 = PlayerColor.Split(",")[1];
                sortedSeats = seats.OrderByDescending(
                               seat => seat.PlayerColor.Equals(winner1, StringComparison.OrdinalIgnoreCase)
                                   || seat.PlayerColor.Equals(winner2, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else
            {
                sortedSeats = seats
                    .OrderByDescending(seat => seat.PlayerColor.Equals(PlayerColor.Split(",")[0], StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            using var context = _contextFactory.CreateDbContext();
            Game existingGame = await context.Games.FirstOrDefaultAsync(g => g.RoomCode == RoomCode);
            existingGame.Winner = sortedSeats[0].PlayerId + "";
            existingGame.State = "Completed";

            // Send the rearranged list to your clients (make sure your client is set up to handle this list)
            await _hubContext.Clients.Group(RoomCode)
            .SendAsync("ShowResults", JsonConvert.SerializeObject(sortedSeats), GameType + "", GameCost + "");

            context.Games.Update(existingGame);
            await context.SaveChangesAsync();            
        }
        public async Task<User> PlayerLeft(string connectionId,string roomCode)
        {
            // Try to find the user in the game room's user list using the connection ID.
            var user = Users.FirstOrDefault(u => u.ConnectionId == connectionId);
            if (user != null)
            {
                // Remove the user from the room.
                Users.Remove(user);

                engine.PlayerLeft(user.PlayerColor);
                
                GameCommand command = new GameCommand
                {
                    SendToClientFunctionName = "PlayerLeft",
                    commandValue1 = user.PlayerColor,
                    commandValue2 = "",
                    commandValue3 = "",
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
        internal async Task<string> MovePieceAsync(string commandValue)
        {
          String piece = await engine.MovePieceAsync(commandValue, false);
            GameCommand command = new GameCommand
            {
                SendToClientFunctionName = "MovePiece",
                commandValue1 = commandValue,
                commandValue2 = "",
                commandValue3 = "",
                Index = engine.EngineHelper.index++
            };
            lock (_commandStoreLock)
                _commandStore.Add(command);

            return piece;
        }
        internal async Task<string> SeatTurn(string commandValue)
        {
            String diveValue = await engine.SeatTurn(commandValue, "", "", false);

            GameCommand command = new GameCommand
            {
                SendToClientFunctionName = "DiceRoll",
                commandValue1 = commandValue,
                commandValue2 = diveValue.Split(",")[0],
                commandValue3 = diveValue.Split(",")[1],
                Index = engine.EngineHelper.index++
            };
            lock (_commandStoreLock)
                _commandStore.Add(command);

            return diveValue;
        }
    }
}