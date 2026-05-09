using LudoClient.Constants;
using LudoClient.Network;
using SharedCode;
using SharedCode.Constants;
using SharedCode.Network;
using System.Runtime.InteropServices;

namespace LudoClient
{
    public partial class App : Application
    {
        public static bool IsInForeground { get; private set; } = true;

        private readonly ClientReceiver _clientReceiver;
        private (string GameType, string seatsData, string rollsString)? _pendingGameStart;
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
            _clientReceiver = new ClientReceiver();
#if WINDOWS
            AllocConsole();
            IntPtr consoleWindow = GetConsoleWindow();
            SetWindowPos(consoleWindow, HWND_TOP, 384, 0, 0, 0, SWP_NOSIZE); // Set position to (100, 100)
            Console.WriteLine("Console started alongside MAUI app at custom position.");
#endif
            InitializeComponent();
            //Preferences.Clear();
            var isUserLoggedIn = Preferences.Get("IsUserLoggedIn", false);
            // Register routes for pages
            //MainPage = new Game();
            UserInfo.LoadState();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (GlobalConstants.MatchMaker == null)
                    GlobalConstants.MatchMaker = new Client();

                RegisterHubEvents();
                _clientReceiver.StartChatPolling();
                
                _ = Task.Run(() => SetOnline(_onlineCts.Token));
                _ = Task.Run(() => _clientReceiver.CheckForResumeNotificationsAsync(_onlineCts.Token));
            });
            MainPage = isUserLoggedIn ? new AppShell() : new LoginPage();
        }
        private void RegisterHubEvents()
        {
            // Unsubscribe first to prevent double-registration if App is re-initialized
            GlobalConstants.MatchMaker.GameStarted -= OnGameStarted;
            GlobalConstants.MatchMaker.PlayerInfoUpdate -= OnPlayerInfoUpdate;

            // Subscribe
            GlobalConstants.MatchMaker.GameStarted += OnGameStarted;
            GlobalConstants.MatchMaker.PlayerInfoUpdate += OnPlayerInfoUpdate;
        }
       
        private void OnPlayerInfoUpdate(object? sender, PlayerInfo newPlayer)
        {   
            if (newPlayer != null)
            {
                var currentPlayer = UserInfo.Instance.player;

                if (currentPlayer != null)
                {
                    currentPlayer.Name = newPlayer.Name;
                    currentPlayer.Email = newPlayer.Email;
                    currentPlayer.Score = newPlayer.Score;

                    if (currentPlayer.Wallet != null && newPlayer.Wallet != null)
                    {
                        currentPlayer.Wallet.AvailableBalance = newPlayer.Wallet.AvailableBalance;
                        currentPlayer.Wallet.ReferBonus = newPlayer.Wallet.ReferBonus;
                        currentPlayer.Wallet.SurpriseCoins = newPlayer.Wallet.SurpriseCoins;
                        currentPlayer.Wallet.SignupBonus = newPlayer.Wallet.SignupBonus;
                    }
                }
                else
                {
                    UserInfo.Instance.player = newPlayer;
                }

                UserInfo.SaveState();
            }
        }
        private void OnGameStarted(object? sender, (string GameType, string seatsData, string rollsString) args)
        {
            Console.WriteLine("Starting Game: " + args.GameType + " " + args.seatsData + " " + args.rollsString);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!IsInForeground)
                {
                    _pendingGameStart = args;
                    return;
                }

                var existingPages = ClientGlobalConstants.dashBoard.Navigation.NavigationStack.ToList();
                if (existingPages.Count == 1)
                    return;
                _clientReceiver.StopCommandPolling();
                try
                {
                    ClientGlobalConstants.game = new LudoClient.CoreEngine.Game();                    
                    ClientGlobalConstants.game.Init("Client", args.GameType, "", args.seatsData, args.rollsString);                   
                    ClientGlobalConstants.dashBoard.Navigation.PushAsync(ClientGlobalConstants.game);
                    ClientGlobalConstants.FlushOld();
                    _clientReceiver.StartCommandPolling();
                }
                catch (Exception)
                {
                    Console.WriteLine("Error starting game: " + args.GameType + " " + args.seatsData + " " + args.rollsString);
                    // Handle the error, e.g., show an alert or log it.
                    
                    ClientGlobalConstants.game?.engine?.cleanGame();
                    ClientGlobalConstants.game = null;
                    GlobalConstants.RoomCode = "";
                    GlobalConstants.GameCost = 0;
                    _clientReceiver.StopCommandPolling();
                }
            });
        }
        private CancellationTokenSource _onlineCts = new();
        protected async Task SetOnline(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var matchMaker = GlobalConstants.MatchMaker;

                    if (matchMaker != null)
                        _ = matchMaker.ConnectAsync();
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SetOnline error: {ex}");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), token);
            }
        }
        protected override void OnSleep()
        {
            Console.WriteLine("App backgrounded");
            IsInForeground = false;
        }
        protected override void OnResume()
        {
            Console.WriteLine("App resumed");
            IsInForeground = true;

            if (_pendingGameStart.HasValue)
            {
                var pending = _pendingGameStart.Value;
                _pendingGameStart = null;
                OnGameStarted(this, pending);
            }
        }
#if WINDOWS
        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = base.CreateWindow(activationState);
            const int newWidth = 400;
            const int newHeight = 800;
            window.Width = newWidth;
            window.Height = newHeight;
            window.X = -5;
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
