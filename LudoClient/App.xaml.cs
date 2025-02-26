
using LudoClient.Constants;
using LudoClient.CoreEngine;
using SharedCode.Constants;
using SharedCode.Network;
using System.Runtime.InteropServices;

namespace LudoClient
{
    public partial class App : Application
    {
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
#if DEBUG
            AllocConsole();
            IntPtr consoleWindow = GetConsoleWindow();
            SetWindowPos(consoleWindow, HWND_TOP, 490, 0, 0, 0, SWP_NOSIZE); // Set position to (100, 100)
            Console.WriteLine("Console started alongside MAUI app at custom position.");
#endif
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
                UserInfo.LoadState();
                GlobalConstants.MatchMaker = new Client();
                GlobalConstants.MatchMaker.RoomJoined += OnRoomJoined;
                GlobalConstants.MatchMaker.GameStarted += OnGameStarted;
                GlobalConstants.MatchMaker.DiceRoll += OnDiceRoll;
                GlobalConstants.MatchMaker.PieceMove += OnPieceMove;

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
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var SeatColor = args.SeatColor;
                var DiceValue = args.DiceValue;
                var Piece = args.Piece;
                ClientGlobalConstants.game.PlayerDiceClicked(SeatColor, DiceValue, Piece, false);
            });
        }

        private void OnPieceMove(object? sender, string Piece)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ClientGlobalConstants.game.PlayerPieceClicked(Piece, false);

            });
        }

        private void OnGameStarted(object? sender, (string GameType, string seatsData) args)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var GameType = args.GameType;
                var seatsData = args.seatsData;
                Game game = new Game(GameType, seatsData);
                ClientGlobalConstants.game = game;
                ClientGlobalConstants.dashBoard.Navigation.PushAsync(game);
                //MainPage = new Game(GameType, seatsData);

                // Retrieve a copy of the current navigation stack.
                var existingPages = ClientGlobalConstants.dashBoard.Navigation.NavigationStack.ToList();

                // Ensure there is at least one page to remove (i.e. the page before the current one).
                if (existingPages.Count > 1)
                {
                    // Remove the page immediately below the current (top) page.
                    ClientGlobalConstants.dashBoard.Navigation.RemovePage(existingPages[existingPages.Count - 2]);
                    existingPages = ClientGlobalConstants.dashBoard.Navigation.NavigationStack.ToList();
                    if(existingPages.Count!=2)
                        ClientGlobalConstants.dashBoard.Navigation.RemovePage(existingPages[existingPages.Count - 2]);
                }
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

                ClientGlobalConstants.dashBoard.Navigation.PushAsync(new GameRoom(gameType, gameCost, roomCode));
                // Retrieve a copy of the current navigation stack.
                var existingPages = ClientGlobalConstants.dashBoard.Navigation.NavigationStack.ToList();

                // Ensure there is at least one page to remove (i.e. the page before the current one).
                if (existingPages.Count > 1)
                {
                    // Remove the page immediately below the current (top) page.
                    ClientGlobalConstants.dashBoard.Navigation.RemovePage(existingPages[existingPages.Count - 2]);
                }
            });
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
}