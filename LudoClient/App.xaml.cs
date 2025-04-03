
using LudoClient.Constants;
using LudoClient.CoreEngine;
using SharedCode.Constants;
using SharedCode.Network;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace LudoClient
{
    public partial class App : Application
    {
        // Static command queue for processing commands sequentially.
        private static readonly ConcurrentQueue<GameCommand> _commandQueue = new ConcurrentQueue<GameCommand>();
        private static bool _processingQueue = false;
        private static bool _processstopper = false;

        //Integrated console to the MAUI app for better debugging
        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();
        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        const uint SWP_NOSIZE = 0x0001;
        static readonly IntPtr HWND_TOP = IntPtr.Zero;
        public App()
        {
#if WINDOWS
            AllocConsole();
            IntPtr consoleWindow = GetConsoleWindow();
            SetWindowPos(consoleWindow, HWND_TOP, 490, 0, 0, 0, SWP_NOSIZE); // Set position to (100, 100)
            Console.WriteLine("Console started alongside MAUI app at custom position.");
#endif
            InitializeComponent();
           // Preferences.Clear();
            var isUserLoggedIn = Preferences.Get("IsUserLoggedIn", false);
            // Register routes for pages
            //MainPage = new Game();
            if (!isUserLoggedIn)
            {
                UserInfo.LoadState(); 
                MainPage = new LoginPage();
            }
            else
            {
                
                GlobalConstants.MatchMaker = new Client();
                GlobalConstants.MatchMaker.RoomJoined += OnRoomJoined;
                GlobalConstants.MatchMaker.GameStarted += OnGameStarted;
                GlobalConstants.MatchMaker.DiceRoll += OnDiceRoll;
                GlobalConstants.MatchMaker.PieceMove += OnPieceMove;
                GlobalConstants.MatchMaker.PlayerLeft += OnPlayerLeft;
                GlobalConstants.MatchMaker.ShowResults += OnShowResults;

                UserInfo.LoadState();
                MainPage = new AppShell();

                //MainPage = new Game("local", "2", "Red");
            }
            //MainPage =new LoginPage();
            //
            //MainPage = new DashboardPage();
            //MainPage = new TabHandeler();
        }
        private void OnDiceRoll(object? sender, (string SeatColor, string DiceValue, string Piece) args)
        {
            GameCommand command = new GameCommand
            {
                CommandType = "DiceRoll",
                SeatColor = args.SeatColor,
                DiceValue = args.DiceValue,
                Piece = args.Piece,
                index = 0
            };

            // Enqueue the command.
            _commandQueue.Enqueue(command);
            // If not already processing, start the process queue.
            if (!_processingQueue)
            {
                _processingQueue = true;
                _ = ProcessQueue(); // Fire-and-forget the queue processor.
            }
        }
        private void OnPieceMove(object? sender, string Piece)
        {
            GameCommand command = new GameCommand
            {
                CommandType = "MovePiece",
                SeatColor = "",
                DiceValue = "",
                Piece = Piece,
                index = 0
            };
            // Enqueue the command.
            _commandQueue.Enqueue(command);
            // If not already processing, start the process queue.
            if (!_processingQueue)
            {
                _processingQueue = true;
                _ = ProcessQueue(); // Fire-and-forget the queue processor.
            }
        }
        private void OnPlayerLeft(object? sender, string PlayerColor)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ClientGlobalConstants.game.engine.PlayerLeft(PlayerColor, false);
            });
        }
        private void OnShowResults(object? sender, (string seats, string GameType, string GameCost) e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ClientGlobalConstants.game.ShowResults(e.seats, e.GameType, e.GameCost);
            });
        }
        private void OnGameStarted(object? sender, (string GameType, string seatsData, string rollsString) args)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Game game = new Game("Client", args.GameType, "", args.seatsData, args.rollsString);
                ClientGlobalConstants.game = game;
                ClientGlobalConstants.dashBoard.Navigation.PushAsync(game);
                //MainPage = new Game(GameType, seatsData);
                ClientGlobalConstants.FlushOld();
            });
        }
        private void OnRoomJoined(object? sender, (string GameType, int GameCost, string RoomCode) args)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var gameType = args.GameType;
                var gameCost = args.GameCost;
                var roomCode = args.RoomCode;
                GlobalConstants.RoomCode = roomCode;
                GlobalConstants.GameCost = gameCost;

                ClientGlobalConstants.dashBoard.Navigation.PushAsync(new GameRoom(gameType, gameCost, roomCode));
                ClientGlobalConstants.FlushOld();
            });
        }
        private async Task ProcessQueue()
        {
            while (_commandQueue.TryDequeue(out GameCommand command))
            {
                _processstopper = true;
                // Optional: add a delay before processing each command.
                await Task.Delay(250); // e.g., 250ms delay
                string result = "0";
                try
                {
                    if (command.CommandType == "MovePiece")
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            ClientGlobalConstants.game.PlayerPieceClicked(command.Piece, false);
                            _processstopper = false;
                        }); 
                    }
                    if (command.CommandType == "DiceRoll")
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            ClientGlobalConstants.game.PlayerDiceClicked(command.SeatColor, command.DiceValue, command.Piece, false);
                            _processstopper = false;
                        });
                    }
                    while (_processstopper)
                    {
                        await Task.Delay(50); // e.g., 50ms delay
                    }
                }
                catch (Exception ex)
                {
                    result = "Error: " + ex.Message;
                }
                await Task.Delay(250); // e.g., 250ms delay
                // Complete the task so the waiting client can continue.
                command.Tcs.SetResult(result);
            }
            _processingQueue = false;
        }

#if WINDOWS
        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = base.CreateWindow(activationState);
            const int newWidth = 400;
            const int newHeight = 800;
            window.Width = newWidth;
            window.Height = newHeight;
            window.X = 0;
            window.Y = 0;
            window.Destroying += Window_Destroying;
            return window;
        }
        private void Window_Destroying(object sender, EventArgs e)
        {
            Window? window = sender as Window;
            try
            {
                System.Diagnostics.Debug.WriteLine(window.X + "Destroying" + window.Y);
            }
            catch (Exception)
            {
            }
            FreeConsole();
        }
#endif
    }
    public class GameCommand
    {
        public string CommandType { get; set; }  // e.g., "MovePiece" or "DiceRoll"
        public string SeatColor { get; set; }
        public string DiceValue { get; set; }
        public string Piece { get; set; }
        public int index { get; set; }
        // TaskCompletionSource allows the sender to await the result.
        public TaskCompletionSource<string> Tcs { get; set; } = new TaskCompletionSource<string>();
    }
}