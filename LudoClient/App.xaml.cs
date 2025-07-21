
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
                GlobalConstants.MatchMaker.RoomJoined += OnRoomJoined;
                GlobalConstants.MatchMaker.GameStarted += OnGameStarted;
                GlobalConstants.MatchMaker.ShowResults += OnShowResults;
                GlobalConstants.MatchMaker.PlayerInfoUpdate += OnPlayerInfoUpdate;
                SetOnline();
            });
            if (isUserLoggedIn)
            {
                MainPage = new AppShell();
                //MainPage = new ChatPage();
                //MainPage = new Game("local", "2", "Red");
            }
            else
            {
                MainPage = new LoginPage();
            }
        }

        private void OnPlayerInfoUpdate(object? sender, PlayerInfo playerInfo)
        {
            UserInfo.Instance.player = playerInfo;
            if (UserInfo.Instance.player != null)
            {
                UserInfo.SaveState();
            }
        }

        private CancellationTokenSource _pollingTokenSource;
        private async Task PollForCommandsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (GlobalConstants.MatchMaker != null && ClientGlobalConstants.game != null && GlobalConstants.RoomCode != null && GlobalConstants.RoomCode != "")
                    {
                        if (GlobalConstants.MatchMaker.Connected && GlobalConstants.MatchMaker._hubConnection.State != HubConnectionState.Disconnected)
                        {
                            // Invoke the hub method to pull commands newer than _lastSeenIndex.
                            int lastSeen = ClientGlobalConstants.game.engine.EngineHelper.indexServer;
                            List<GameCommand> commands = await GlobalConstants.MatchMaker.PullCommands(lastSeen, GlobalConstants.RoomCode);

                            if (commands != null && commands.Count > 0)
                            {
                                foreach (var command in commands.OrderBy(c => c.IndexServer))
                                {
                                    while (ClientGlobalConstants.game.engine.processing)
                                        await Task.Delay(100);

                                    //  Console.WriteLine($"Room {GlobalConstants.RoomCode} LastSeenIndex {ClientGlobalConstants.game.engine.EngineHelper.index} Received Command Index: {command.Index}, Type: {command.SendToClientFunctionName}, Value1: {command.commandValue1},{command.commandValue2},{command.commandValue3}");
                                    // Process the command here (e.g., call a local method based on the command type).
                                    // Update _lastSeenIndex with the highest received index.
                                    if (ClientGlobalConstants.game.engine.EngineHelper.index <= command.Index)
                                    {
                                        switch (command.SendToClientFunctionName)
                                        {
                                            case "MovePiece":
                                                MainThread.BeginInvokeOnMainThread(async () =>
                                                {
                                                    //if (!ClientGlobalConstants.game.playerColor.ToLower().Contains(Piece1.Replace("1", "").Replace("2", "").Replace("3", "").Replace("4", "")))
                                                    await ClientGlobalConstants.game.MovePiece(command.piece1, command.piece2, false);
                                                });
                                                break;
                                            case "DiceRoll":
                                                // For other command types, for example, SeatTurn:
                                                // If SeatTurn returns a string, you can wait for it.
                                                MainThread.BeginInvokeOnMainThread(() =>
                                                {
                                                    //if (ClientGlobalConstants.game.playerColor.ToLower() != args.SeatColor)
                                                    ClientGlobalConstants.game.PlayerDiceClicked(command.seatName, command.diceValue, command.piece1, command.piece2, false);
                                                });
                                                break;
                                            case "PlayerLeft":
                                                MainThread.BeginInvokeOnMainThread(() =>
                                                {
                                                    ClientGlobalConstants.game.engine.EngineHelper.indexServer++;
                                                    ClientGlobalConstants.game.engine.EngineHelper.index++;
                                                    if (ClientGlobalConstants.game != null)
                                                        ClientGlobalConstants.game.engine.PlayerLeft(command.seatName, false);

                                                    ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("left");
                                                });
                                                break;
                                        }
                                        // Wait a bit before polling again.
                                        await Task.Delay(200);
                                    }
                                }

                                if (commands.Any())
                                    ClientGlobalConstants.game.engine.EngineHelper.indexServer = commands.Max(c => c.IndexServer);
                            }
                            if(ClientGlobalConstants.game!=null)
                                if (lastSeen != ClientGlobalConstants.game.engine.EngineHelper.index)
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
                ClientGlobalConstants.GoBack();
                await ClientGlobalConstants.game.ShowResults(e.seats, e.GameType, e.GameCost);

                ClientGlobalConstants.game.engine.cleanGame();
                ClientGlobalConstants.game = null;
                GlobalConstants.RoomCode = "";
                GlobalConstants.GameCost = 0;
            });
        }
        private void OnGameStarted(object? sender, (string GameType, string seatsData, string rollsString) args)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var existingPages = ClientGlobalConstants.dashBoard.Navigation.NavigationStack.ToList();
                if (existingPages.Count == 1)
                    return;
                // Remove all pages except the first one (which is the dashboard).
                _pollingTokenSource = new CancellationTokenSource();
                try
                {
                    GlobalConstants.lastSeenIndex = -1;
                    ClientGlobalConstants.game = new LudoClient.CoreEngine.Game("Client", args.GameType, "", args.seatsData, args.rollsString);
                    ClientGlobalConstants.dashBoard.Navigation.PushAsync(ClientGlobalConstants.game);
                    ClientGlobalConstants.FlushOld();
                    Task.Run(() => PollForCommandsAsync(_pollingTokenSource.Token));
                }
                catch (Exception)
                {
                    Console.WriteLine("Error starting game: " + args.GameType + " " + args.seatsData + " " + args.rollsString);
                    // Handle the error, e.g., show an alert or log it.
                    ClientGlobalConstants.game.engine.cleanGame();
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
        private void OnRoomJoined(object? sender, (string GameType, double GameCost, string RoomCode) args)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                GlobalConstants.RoomCode = args.RoomCode;
                GlobalConstants.GameCost = args.GameCost;

                ClientGlobalConstants.dashBoard.Navigation.PushAsync(new GameRoom(args.GameType, args.GameCost, args.RoomCode));
                ClientGlobalConstants.FlushOld();
            });
        }
        protected async Task SetOnline()
        {
            while (true)
            {
                if (GlobalConstants.MatchMaker != null && GlobalConstants.MatchMaker.Connected && GlobalConstants.MatchMaker._hubConnection.State != HubConnectionState.Disconnected)
                {
                    try
                    {
                        UserInfo.Instance.player = await GlobalConstants.MatchMaker.UserConnectedSetID();
                        if (UserInfo.Instance.player != null)
                        {
                            UserInfo.SaveState();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
                await Task.Delay(TimeSpan.FromMinutes(1));
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