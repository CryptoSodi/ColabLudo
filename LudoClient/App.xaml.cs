using LudoClient.Constants;
using Microsoft.AspNetCore.SignalR.Client;
using SharedCode;
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
                GlobalConstants.MatchMaker = new Client();
                GlobalConstants.MatchMaker.GameStarted += OnGameStarted;
                GlobalConstants.MatchMaker.ShowResults += OnShowResults;
                GlobalConstants.MatchMaker.PlayerInfoUpdate += OnPlayerInfoUpdate;
                _ = Task.Run(() => SetOnline(_onlineCts.Token));
            });
            if (isUserLoggedIn)
            {
                MainPage = new AppShell();
            }
            else
            {
                MainPage = new LoginPage();
            }
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
        private CancellationTokenSource _pollingTokenSource { get; set; }
        private async Task PollForCommandsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var game = ClientGlobalConstants.game; 
                    if (game == null)
                        break;
                    if (GlobalConstants.MatchMaker != null && game != null && !string.IsNullOrEmpty(GlobalConstants.RoomCode))
                    {
                        if (GlobalConstants.MatchMaker.Connected && GlobalConstants.MatchMaker._hubConnection.State != HubConnectionState.Disconnected)
                        {
                            // Invoke the hub method to pull commands newer than _lastSeenIndex.
                            int lastSeen = game.engine.EngineHelper.indexServer;
                            List<GameCommand> commands = await GlobalConstants.MatchMaker.PullCommands(lastSeen, GlobalConstants.RoomCode);

                            if (commands?.Count > 0)
                            {
                                foreach (var command in commands.OrderBy(c => c.IndexServer))
                                {
                                    game = ClientGlobalConstants.game;
                                    if (game == null)
                                        break;
                                    while (game != null && game.engine.processing)
                                    {
                                        cancellationToken.ThrowIfCancellationRequested();
                                        await Task.Delay(100, cancellationToken);
                                    }

                                    //  Console.WriteLine($"Room {GlobalConstants.RoomCode} LastSeenIndex {ClientGlobalConstants.game.engine.EngineHelper.index} Received Command Index: {command.Index}, Type: {command.SendToClientFunctionName}, Value1: {command.commandValue1},{command.commandValue2},{command.commandValue3}");
                                    // Process the command here (e.g., call a local method based on the command type).
                                    // Update _lastSeenIndex with the highest received index.
                                    if (game != null && game.engine.EngineHelper.index <= command.Index)
                                    {
                                        await MainThread.InvokeOnMainThreadAsync(async () =>
                                        {
                                            while (game.engine.processing || game.isInputLocked) await Task.Delay(10);
                                            try
                                            {
                                                switch (command.SendToClientFunctionName)
                                                {
                                                    case "MovePiece":
                                                        //if (!ClientGlobalConstants.game.playerColor.ToLower().Contains(Piece1.Replace("1", "").Replace("2", "").Replace("3", "").Replace("4", "")))
                                                        if (command.piece1 != null && command.piece2 != null)
                                                            await game.MovePiece(command.piece1, command.piece2, false);
                                                        break;
                                                    case "DiceRoll":
                                                        // For other command types, for example, SeatTurn:
                                                        // If SeatTurn returns a string, you can wait for it.
                                                        //if (ClientGlobalConstants.game.playerColor.ToLower() != args.SeatColor)
                                                        DiceRoll:
                                                        if (command.seatName != null && command.diceValue != null && command.piece1 != null && command.piece2 != null) { 
                                                            string result = await game.PlayerDiceClicked(command.seatName, command.diceValue, command.piece1, command.piece2, false);
                                                        if(result == "-2")
                                                            {
                                                                await Task.Delay(100);
                                                                goto DiceRoll;
                                                            }
                                                            else if(result.Contains("-1") || result.Contains("-0"))
                                                            {
                                                                //Command failed execution failed on the client
                                                            }
                                                            else
                                                            {
                                                                //Command executed successfully on the client
                                                                game._commandStore.Add(command);
                                                            }
                                                        }
                                                        break;
                                                    case "PlayerLeft":
                                                        if (game != null && command.seatName != null)
                                                            await game.engine.PlayerLeft(command.seatName, false);
                                                        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("left");
                                                        break;
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine($"ERROR IN SWITCH : 001 {ex.Message}");
                                            }
                                        });
                                        // Wait a bit before polling again.
                                        game.engine.EngineHelper.indexServer = command.IndexServer;
                                        await Task.Delay(200, cancellationToken);
                                    }
                                }
                            }
                            if (game != null)
                                if (lastSeen != game.engine.EngineHelper.index)
                                    Console.WriteLine("DESYNC WARNING!");
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    // Normal exit
                    Console.WriteLine($"Error pulling commands: EXIT 101");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error pulling commands: {ex.Message}");
                }
                await Task.Delay(1000, cancellationToken); // Polling interval - also cancellable
            }
        }
        private void OnShowResults(object? sender, (string seats, string GameType, string GameCost) e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (_pollingTokenSource != null)
                {
                    _pollingTokenSource.Cancel();
                    _pollingTokenSource.Dispose();
                    _pollingTokenSource = null;
                }
                
                if(ClientGlobalConstants.game == null)
                    return;
                //ClientGlobalConstants.GoBack();
                await ClientGlobalConstants.game.ShowResults(e.seats, e.GameType, e.GameCost);

                ClientGlobalConstants.game.engine.cleanGame();
                ClientGlobalConstants.game = null;
                GlobalConstants.RoomCode = "";
                GlobalConstants.GameCost = 0;
            });
        }
        private void OnGameStarted(object? sender, (string GameType, string seatsData, string rollsString) args)
        {
            Console.WriteLine("Starting Game: " + args.GameType + " " + args.seatsData + " " + args.rollsString);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var existingPages = ClientGlobalConstants.dashBoard.Navigation.NavigationStack.ToList();
                if (existingPages.Count == 1)
                    return;
                // Cancel previous polling loop if it exists
                _pollingTokenSource?.Cancel();
                _pollingTokenSource?.Dispose();
                // Remove all pages except the first one (which is the dashboard).
                _pollingTokenSource = new CancellationTokenSource();
                try
                {
                    ClientGlobalConstants.game = new LudoClient.CoreEngine.Game();                    
                    ClientGlobalConstants.game.Init("Client", args.GameType, "", args.seatsData, args.rollsString);                   
                    ClientGlobalConstants.dashBoard.Navigation.PushAsync(ClientGlobalConstants.game);
                    ClientGlobalConstants.FlushOld();
                    _ = PollForCommandsAsync(_pollingTokenSource.Token);
                }
                catch (Exception)
                {
                    Console.WriteLine("Error starting game: " + args.GameType + " " + args.seatsData + " " + args.rollsString);
                    // Handle the error, e.g., show an alert or log it.
                    
                    ClientGlobalConstants.game?.engine?.cleanGame();
                    ClientGlobalConstants.game = null;
                    GlobalConstants.RoomCode = "";
                    GlobalConstants.GameCost = 0;
                    if(_pollingTokenSource != null)
                    {
                        _pollingTokenSource.Cancel();
                        _pollingTokenSource.Dispose();
                        _pollingTokenSource = null;
                    }
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

                    if (matchMaker?.Connected == true && matchMaker._hubConnection.State != HubConnectionState.Disconnected)
                    {
                        var newPlayer = await matchMaker.UserConnectedSetID();
                        if (newPlayer != null)
                            OnPlayerInfoUpdate(null, newPlayer);
                    }
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
        }
        protected override void OnResume()
        {
            Console.WriteLine("App resumed");
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