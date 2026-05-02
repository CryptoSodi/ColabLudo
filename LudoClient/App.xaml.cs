using LudoClient.Constants;
using SharedCode;
using SharedCode.Constants;
using SharedCode.Network;
using System.Runtime.InteropServices;

namespace LudoClient
{
    public partial class App : Application
    {
        public static bool IsInForeground { get; private set; } = true;

        private List<NotificationDTO> _pendingNotifications { get; set; } = new();
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
                
                _ = Task.Run(() => SetOnline(_onlineCts.Token));
                _ = Task.Run(() => CheckForResumeNotifications(_onlineCts.Token));
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
        private void RegisterHubEvents()
        {
            // Unsubscribe first to prevent double-registration if App is re-initialized
            GlobalConstants.MatchMaker.GameStarted -= OnGameStarted;
            GlobalConstants.MatchMaker.ShowResults -= OnShowResults;
            GlobalConstants.MatchMaker.PlayerInfoUpdate -= OnPlayerInfoUpdate;
            GlobalConstants.MatchMaker.ReceiveNotification -= OnReceiveNotification;
            GlobalConstants.MatchMaker.ReceiveChatMessage -= OnReceiveChatMessage;

            // Subscribe
            GlobalConstants.MatchMaker.GameStarted += OnGameStarted;
            GlobalConstants.MatchMaker.ShowResults += OnShowResults;
            GlobalConstants.MatchMaker.PlayerInfoUpdate += OnPlayerInfoUpdate;
            GlobalConstants.MatchMaker.ReceiveNotification += OnReceiveNotification;
            GlobalConstants.MatchMaker.ReceiveChatMessage += OnReceiveChatMessage;
        }
        private void OnReceiveChatMessage(object? sender, List<ChatMessages> messages)
        {
            // HISTORY CHECK: If multiple messages arrive at once, it's a history fetch.
            // We should NOT trigger a notification for history.
            if (messages == null || messages.Count != 1) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var latestMsg = messages.First();
                
                // Only notify for PRIVATE messages (no room code)
                if (!string.IsNullOrEmpty(latestMsg.RoomCode)) return;

                int activeChatId = GetActiveChatPlayerId();

                // RULE: If we are ALREADY chatting with this person, ignore notification entirely
                if (activeChatId == latestMsg.SenderId)
                    return;

                // SUPPRESSION: Queue if in a game or on a chat page
                if (!string.IsNullOrEmpty(GlobalConstants.RoomCode) || activeChatId != -1)
                {
                    lock (_pendingNotifications)
                    {
                        var note = new NotificationDTO
                        {
                            Title = latestMsg.SenderName ?? "New Message",
                            Message = latestMsg.Message ?? "",
                            Type = "Message",
                            Payload = latestMsg.SenderId.ToString()
                        };
                        
                        // Prevent spamming the queue with the same message
                        if (!_pendingNotifications.Any(n => n.Payload == note.Payload && n.Message == note.Message))
                        {
                            _pendingNotifications.Add(note);
                        }
                    }
                    return;
                }

                // Show immediately if in lobby
                var notification = new NotificationDTO
                {
                    Title = latestMsg.SenderName ?? "New Message",
                    Message = latestMsg.Message ?? "",
                    Type = "Message",
                    Payload = latestMsg.SenderId.ToString()
                };
                ShowNotification(notification);
            });
        }
        private async Task CheckForResumeNotifications(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (string.IsNullOrEmpty(GlobalConstants.RoomCode))
                {
                    int activeChatId = GetActiveChatPlayerId();
                    if (activeChatId != -1)
                    {
                        lock (_pendingNotifications)
                        {
                            // Clear queue for the person we are currently talking to
                            _pendingNotifications.RemoveAll(n => n.Type == "Message" && n.Payload == activeChatId.ToString());
                        }
                    }

                    if (_pendingNotifications.Count > 0)
                    {
                        await ProcessPendingNotifications();
                    }
                }
                await Task.Delay(2000, token); // Check every 2 seconds
            }
        }
        private int GetActiveChatPlayerId()
        {
            try
            {
                if (MainPage is AppShell shell)
                {
                    var stack = shell.Navigation.NavigationStack;
                    if (stack.Count > 0 && stack.Last() is ChatPage cp)
                    {
                        return cp.playerCard.playerID;
                    }
                }
            }
            catch { }
            return -1;
        }
        private async Task ProcessPendingNotifications()
        {
            List<NotificationDTO> toProcess;
            lock (_pendingNotifications)
            {
                int activeChatId = GetActiveChatPlayerId();
                // Last second check: don't show notifications for the open chat
                toProcess = _pendingNotifications.Where(n => 
                    !(n.Type == "Message" && n.Payload == activeChatId.ToString())
                ).ToList();
                
                _pendingNotifications.Clear();
            }

            foreach (var note in toProcess)
            {
                ShowNotification(note);
                await Task.Delay(1500);
            }
        }
        private void OnReceiveNotification(object? sender, NotificationDTO notification)
        {
            // SUPPRESSION LOGIC: Don't show notifications if in a game or waiting room
            if (!string.IsNullOrEmpty(GlobalConstants.RoomCode))
            {
                lock (_pendingNotifications)
                {
                    if (!_pendingNotifications.Any(n => n.Payload == notification.Payload && n.Message == notification.Message))
                    {
                        _pendingNotifications.Add(notification);
                    }
                }
                return;
            }

            ShowNotification(notification);
        }
        private void ShowNotification(NotificationDTO notification)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var snackbarOptions = new CommunityToolkit.Maui.Core.SnackbarOptions
                {
                    BackgroundColor = Color.FromArgb("#CC143450"), // Semi-transparent Ludo Blue
                    TextColor = Colors.White,
                    ActionButtonTextColor = Colors.Yellow,
                    CornerRadius = new CornerRadius(10),
                    Font = Microsoft.Maui.Font.SystemFontOfSize(14),
                    ActionButtonFont = Microsoft.Maui.Font.SystemFontOfSize(14, Microsoft.Maui.FontWeight.Bold)
                };

                var snackbar = CommunityToolkit.Maui.Alerts.Snackbar.Make(
                    $"{notification.Title}\n{notification.Message}",
                    async () => {
                    try 
                    {
                        if (notification.Type == "Message")
                        {
                            int senderId = int.Parse(notification.Payload);
                            var playerCard = await GlobalConstants.MatchMaker.GetPlayerById(senderId);
                            if (playerCard != null)
                            {
                                // Robust navigation using Shell
                                await MainThread.InvokeOnMainThreadAsync(async () => {
                                    await Shell.Current.Navigation.PushAsync(new ChatPage(playerCard));
                                });
                            }
                        }
                        else if (notification.Type == "TournamentResults")
                        {
                            await MainThread.InvokeOnMainThreadAsync(async () => {
                                await Shell.Current.GoToAsync("//LeaderboardPage");
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error navigating from notification: {ex.Message}");
                    }
                    },
                    "OPEN",
                    TimeSpan.FromSeconds(5),
                    snackbarOptions);

                await snackbar.Show();
            });
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
                        if (GlobalConstants.MatchMaker.Connected)
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
                                    bool alreadyHandled = game._commandStore.Any(c => c.IndexServer == command.IndexServer);
                                    if (game != null && !alreadyHandled)
                                    {
                                        await MainThread.InvokeOnMainThreadAsync(async () =>
                                        {
                                            while (game.engine.processing || game.isInputLocked) await Task.Delay(10);
                                            try
                                            {
                                                switch (command.SendToClientFunctionName)
                                                {
                                                    case "MovePiece":
                                                    MovePiece:                                                        
                                                        if (command.piece1 != null && command.piece2 != null)
                                                        {
                                                            string result = await game.MovePiece(command.piece1, command.piece2, false);
                                                            if (result == "-2")
                                                            {
                                                                await Task.Delay(100);
                                                                goto MovePiece;
                                                            }
                                                            else if (!result.Contains("-1") && !result.Contains("-0"))
                                                            {
                                                                game._commandStore.Add(command);
                                                            }
                                                        }
                                                        break;
                                                    case "DiceRoll":
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
                                                        {
                                                            PlayerLeft:
                                                            string result = await game.PlayerLeft(command.seatName, false);
                                                            if (result == "-2")
                                                            {
                                                                await Task.Delay(100);
                                                                goto PlayerLeft;
                                                            }
                                                            else if (result.Contains("-1") || result.Contains("-0"))
                                                            {
                                                                //Command failed execution failed on the client
                                                            }
                                                            else
                                                            {
                                                                //Command executed successfully on the client
                                                                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("left");
                                                                game._commandStore.Add(command);
                                                            }
                                                        }
                                                        break;
                                                    case "ShowResults":
                                                        await OnShowResultsFromCommand(command);
                                                        game._commandStore.Add(command);
                                                        break;
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine($"ERROR IN SWITCH : 001 {ex.Message}");
                                            }
                                        });
                                        // Wait a bit before polling again.
                                        await Task.Delay(200, cancellationToken);
                                    }
                                    Console.WriteLine($"Sync states Handled : {alreadyHandled} Index : {game.engine.EngineHelper.index} LoclServerIndex {game.engine.EngineHelper.indexServer} ServerIndex {command.IndexServer}");
                                }
                            }
                            if (game != null)
                                if (lastSeen != game.engine.EngineHelper.index)
                                {
                                    Console.WriteLine($"Sync X 2 states Index : {game.engine.EngineHelper.index} LoclServerIndex {game.engine.EngineHelper.indexServer}");
                                }
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
        private async Task OnShowResultsFromCommand(GameCommand command)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                OnShowResults(this, (
                    command.ShowResultsSeats ?? string.Empty,
                    command.ShowResultsGameType ?? string.Empty,
                    command.ShowResultsGameCost ?? string.Empty));
                await Task.CompletedTask;
            });
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
