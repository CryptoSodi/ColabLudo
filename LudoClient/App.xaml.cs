
using LudoClient.Constants;
using LudoClient.CoreEngine;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Maui.Controls;
using SharedCode;
using SharedCode.Constants;
using SharedCode.Network;
using System.Collections.Concurrent;
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
            // Start polling in a background thread using Task.Run.
            Task.Run(async () =>
            {
                await PollForCommandsAsync();
            });
        }
        private void OnDiceRoll(object? sender, (string SeatColor, string DiceValue, string Piece) args)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ClientGlobalConstants.game.engine.EngineHelper.index++;
                if (ClientGlobalConstants.game.playerColor.ToLower() != args.SeatColor)
                    ClientGlobalConstants.game.PlayerDiceClicked(args.SeatColor, args.DiceValue, args.Piece, false);
            });
        }
        private void OnPieceMove(object? sender, string Piece)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ClientGlobalConstants.game.engine.EngineHelper.index++;
                if (!ClientGlobalConstants.game.playerColor.ToLower().Contains(Piece.Replace("1", "").Replace("2", "").Replace("3", "").Replace("4", "")))
                    ClientGlobalConstants.game.PlayerPieceClicked(Piece, false);
            });
        }

        private int _lastSeenIndex = -1; // Start at -1 so that the first poll returns all commands.
        private async Task PollForCommandsAsync()
        {
            while (true)
            {
                try
                {
                    if (GlobalConstants.MatchMaker != null)
                    {
                        // Invoke the hub method to pull commands newer than _lastSeenIndex.
                        List<GameCommand> commands = await GlobalConstants.MatchMaker.PullCommands(_lastSeenIndex);
                        {
                            if (commands != null && commands.Count > 0)
                                foreach (var command in commands)
                                {
                                    while (ClientGlobalConstants.game.engine.processing)
                                    {
                                        await Task.Delay(100);
                                    }
                                    Console.WriteLine($"Received Command Index: {command.Index}, Type: {command.SendToClientFunctionName}, Value1: {command.commandValue1},{command.commandValue2},{command.commandValue3}");
                                    // Process the command here (e.g., call a local method based on the command type).
                                    // Update _lastSeenIndex with the highest received index.
                                    _lastSeenIndex = command.Index;
                                    if (command.SendToClientFunctionName == "MovePiece")
                                    {
                                        OnPieceMove(this, command.commandValue1);
                                    }
                                    else if (command.SendToClientFunctionName == "DiceRoll")
                                    {
                                        // For other command types, for example, SeatTurn:
                                        // If SeatTurn returns a string, you can wait for it.
                                        OnDiceRoll(this, (command.commandValue1, command.commandValue2, command.commandValue3));
                                    }
                                }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error pulling commands: {ex.Message}");
                }
                // Wait a bit before polling again.
                await Task.Delay(500);
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