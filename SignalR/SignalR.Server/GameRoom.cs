using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using SharedCode;
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

        public List<PlayerDto> seats = new List<PlayerDto>();
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
            engine = new Engine("Server", GameType, Users.Count.ToString(), initialPlayerColor);
            engine.ShowResults += new Engine.CallbackEventHandlerShowResults(ShowResults);

            engine.StartProgressAnimation += StartProgressAnimation;
            engine.StopProgressAnimation += StopProgressAnimation;
            TimerTimeout += engine.TimerTimeoutAsync;
            
            StartProgressAnimation("");
            //engine.TimerTimeoutAsync(engine.EngineHelper.currentPlayer.Color);
        }
        private async Task ShowResults(string PlayerColor)
        {
            // Instead of Thread.Sleep, use Task.Delay for async waiting.
            await Task.Delay(2000);
            // Assume 'seats' is a List<Seat> and Seat has a property 'SeatColor'
            // Order the list so that seats whose SeatColor equals the provided seatColor come first.
            var sortedSeats = seats
                .OrderByDescending(seat => seat.PlayerColor.Equals(PlayerColor, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Send the rearranged list to your clients (make sure your client is set up to handle this list)
            await Clients.Group(RoomName).SendAsync("ShowResults", JsonConvert.SerializeObject(sortedSeats), GameType + "", GameCost + "");
        }
        public async Task<User> PlayerLeft(string connectionId,string roomCode)
        {
            // Try to find the user in the game room's user list using the connection ID.
            var user = Users.FirstOrDefault(u => u.ConnectionId == connectionId);
            if (user != null)
            {
                // Remove the user from the room.
                Users.Remove(user);

                // Optionally, perform additional cleanup or update the game engine state.
                // For example: engine.RemoveUser(user); // if your engine supports this
                await Clients.Group(roomCode).SendAsync("PlayerLeft", user.PlayerColor);
                // Notify all connected clients that a user has left.
                engine.PlayerLeft(user.PlayerColor);
                Console.WriteLine("User removed: " + user.PlayerColor);
            }
            else
            {
                Console.WriteLine("User not found for connection: " + connectionId);
            }
            return user;
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