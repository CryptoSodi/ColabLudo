
using LudoClient.Constants;
using LudoClient.CoreEngine;
using Microsoft.Maui.Controls;
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
                GlobalConstants.MatchMaker.ShowResults += OnShowResults;

                UserInfo.LoadState();
                MainPage = new AppShell();

                //MainPage = new Game("local", "2", "Red");
            }
            //MainPage =new LoginPage();
            //
            //MainPage = new DashboardPage();
            //MainPage = new TabHandeler();
            // Start polling in a background thread using Task.Run.
            Task.Run(async () =>
            {
                await PollForCommandsAsync();
            });
        }
        private void OnDiceRoll(object? sender, (string SeatColor, string DiceValue, string Piece1, string Piece2) args)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (ClientGlobalConstants.game.playerColor.ToLower() != args.SeatColor)
                    ClientGlobalConstants.game.PlayerDiceClicked(args.SeatColor, args.DiceValue, args.Piece1, args.Piece2, false);
            });
        }
        private void OnPieceMove(object? sender, string Piece1, string Piece2)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!ClientGlobalConstants.game.playerColor.ToLower().Contains(Piece1.Replace("1", "").Replace("2", "").Replace("3", "").Replace("4", "")))
                    ClientGlobalConstants.game.PlayerPieceClicked(Piece1, Piece2, false);
            });
        }

      
        private async Task PollForCommandsAsync()
        {
            while (true)
            {
                try
                {
                    if (GlobalConstants.MatchMaker != null && ClientGlobalConstants.game != null && GlobalConstants.RoomCode != null && GlobalConstants.RoomCode != "")
                    {
                        if (GlobalConstants.MatchMaker.Connected && GlobalConstants.MatchMaker._hubConnection.State + "" != "Disconnected")
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
                                                OnPieceMove(this, command.piece1, command.piece2);
                                                break;
                                            case "DiceRoll":
                                                // For other command types, for example, SeatTurn:
                                                // If SeatTurn returns a string, you can wait for it.
                                                OnDiceRoll(this, (command.seatName, command.diceValue, command.piece1, command.piece2));
                                                break;
                                            case "PlayerLeft":
                                                OnPlayerLeft(this, command.seatName);
                                                break;
                                        }
                                        // Wait a bit before polling again.
                                        await Task.Delay(500);
                                    }
                                }

                                if (commands.Any())
                                    ClientGlobalConstants.game.engine.EngineHelper.indexServer = commands.Max(c => c.IndexServer);
                            }

                            if (lastSeen != ClientGlobalConstants.game.engine.EngineHelper.index)
                                Console.WriteLine("DESYNC WARNING!");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error pulling commands: {ex.Message}");
                }
                // Wait a bit before polling again.
                await Task.Delay(1000);
            }
        }
        private void OnPlayerLeft(object? sender, string PlayerColor)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ClientGlobalConstants.game.engine.EngineHelper.index++;
                if (ClientGlobalConstants.game != null)
                    ClientGlobalConstants.game.engine.PlayerLeft(PlayerColor, false);
            });
        }
        private void OnShowResults(object? sender, (string seats, string GameType, string GameCost) e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
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
                GlobalConstants.lastSeenIndex = -1;
                ClientGlobalConstants.game = new Game("Client", args.GameType, "", args.seatsData, args.rollsString);
                ClientGlobalConstants.dashBoard.Navigation.PushAsync(ClientGlobalConstants.game);
                ClientGlobalConstants.FlushOld();
            });
        }
        private void OnRoomJoined(object? sender, (string GameType, int GameCost, string RoomCode) args)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                GlobalConstants.RoomCode = args.RoomCode;
                GlobalConstants.GameCost = args.GameCost;

                ClientGlobalConstants.dashBoard.Navigation.PushAsync(new GameRoom(args.GameType, args.GameCost, args.RoomCode));
                ClientGlobalConstants.FlushOld();
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