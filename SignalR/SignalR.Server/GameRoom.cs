using SharedCode.CoreEngine;

namespace SignalR.Server
{
    public class GameRoom
    {
        Microsoft.AspNetCore.SignalR.IHubCallerClients Clients;
        LudoServer.Data.LudoDbContext _context;
        public string RoomName { get; set; }
        public string GameType { get; set; }
        public decimal GameCost { get; set; }
        public List<User> Users { get; set; }
        public Engine engine { get; set; }  // The Engine instance for this room
        public GameRoom(Microsoft.AspNetCore.SignalR.IHubCallerClients clients, LudoServer.Data.LudoDbContext _context, string roomName, string gameType, decimal gameCost)
        {
            this.Clients = clients;
            this._context = _context;
            RoomName = roomName;
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
            engine = new Engine(GameType, Users.Count.ToString(), initialPlayerColor);

            engine.StartProgressAnimation += StartProgressAnimation;
            engine.StopProgressAnimation += StopProgressAnimation;
            TimerTimeout += engine.TimerTimeoutAsync;
            StartProgressAnimation("");
            //engine.TimerTimeoutAsync(engine.EngineHelper.currentPlayer.Color);
        }
        public async Task StartProgressAnimationAsync(string SeatName)
        {
            // Wait until the component has rendered
            // Cancel any previous animation
            StopProgressAnimation("");
            _animationCancellationTokenSource = new CancellationTokenSource();
            await AnimateProgress(_animationCancellationTokenSource.Token);
        }
        public void StartProgressAnimation(string SeatName)
        {
            StartProgressAnimationAsync(engine.EngineHelper.currentPlayer.Color);
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
    }
}