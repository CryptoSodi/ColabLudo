
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

                MainPage = new AppShell();
                //MainPage = new Game();
            }
            //MainPage =new LoginPage();
            //
            //MainPage = new DashboardPage();
            //MainPage = new TabHandeler();
        }
        private void OnGameStarted(object? sender, (string GameType, string seatsData) args)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var GameType = args.GameType;
                var seatsData = args.seatsData;

                MainPage = new Game(GameType, seatsData);
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
                MainPage = new GameRoom(gameType, gameCost, roomCode);
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