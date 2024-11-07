
using LudoClient.Constants;
using LudoClient.ControlView;
using LudoClient.Network;

namespace LudoClient.CoreEngine
{
    public class Engine
    {
        // Events
        public delegate void CallbackEventHandler(string SeatName, int diceValue);
        public event CallbackEventHandler StopDice;
        // Game logic helpers
        private static Dictionary<string, List<Piece>> board;
        string playerColor; 
        private Timer _timer;
        bool run = false;
        public Engine(string gameType, string playerCount, string playerColor, Gui gui, Capsule Glayout, AbsoluteLayout Alayout)
        {
            this.playerColor = playerColor;
            board = new Dictionary<string, List<Piece>>
    {
        { "p0", new List<Piece>() },
        { "p1", new List<Piece>() },
        { "p2", new List<Piece>() },
        { "p3", new List<Piece>() },
        { "p4", new List<Piece>() },
        { "p5", new List<Piece>() },
        { "p6", new List<Piece>() },
        { "p7", new List<Piece>() },
        { "p8", new List<Piece>() },
        { "p9", new List<Piece>() },
        { "p10", new List<Piece>() },
        { "p11", new List<Piece>() },
        { "p12", new List<Piece>() },
        { "p13", new List<Piece>() },
        { "p14", new List<Piece>() },
        { "p15", new List<Piece>() },
        { "p16", new List<Piece>() },
        { "p17", new List<Piece>() },
        { "p18", new List<Piece>() },
        { "p19", new List<Piece>() },
        { "p20", new List<Piece>() },
        { "p21", new List<Piece>() },
        { "p22", new List<Piece>() },
        { "p23", new List<Piece>() },
        { "p24", new List<Piece>() },
        { "p25", new List<Piece>() },
        { "p26", new List<Piece>() },
        { "p27", new List<Piece>() },
        { "p28", new List<Piece>() },
        { "p29", new List<Piece>() },
        { "p30", new List<Piece>() },
        { "p31", new List<Piece>() },
        { "p32", new List<Piece>() },
        { "p33", new List<Piece>() },
        { "p34", new List<Piece>() },
        { "p35", new List<Piece>() },
        { "p36", new List<Piece>() },
        { "p37", new List<Piece>() },
        { "p38", new List<Piece>() },
        { "p39", new List<Piece>() },
        { "p40", new List<Piece>() },
        { "p41", new List<Piece>() },
        { "p42", new List<Piece>() },
        { "p43", new List<Piece>() },
        { "p44", new List<Piece>() },
        { "p45", new List<Piece>() },
        { "p46", new List<Piece>() },
        { "p47", new List<Piece>() },
        { "p48", new List<Piece>() },
        { "p49", new List<Piece>() },
        { "p50", new List<Piece>() },
        { "p51", new List<Piece>() },
        { "r51", new List<Piece>() },
        { "r52", new List<Piece>() },
        { "r53", new List<Piece>() },
        { "r54", new List<Piece>() },
        { "r55", new List<Piece>() },
        { "r56", new List<Piece>() },
        { "g51", new List<Piece>() },
        { "g52", new List<Piece>() },
        { "g53", new List<Piece>() },
        { "g54", new List<Piece>() },
        { "g55", new List<Piece>() },
        { "g56", new List<Piece>() },
        { "y51", new List<Piece>() },
        { "y52", new List<Piece>() },
        { "y53", new List<Piece>() },
        { "y54", new List<Piece>() },
        { "y55", new List<Piece>() },
        { "y56", new List<Piece>() },
        { "b51", new List<Piece>() },
        { "b52", new List<Piece>() },
        { "b53", new List<Piece>() },
        { "b54", new List<Piece>() },
        { "b55", new List<Piece>() },
        { "b56", new List<Piece>() },
        { "hr0", new List<Piece>() },
        { "hr1", new List<Piece>() },
        { "hr2", new List<Piece>() },
        { "hr3", new List<Piece>() },
        { "hg0", new List<Piece>() },
        { "hg1", new List<Piece>() },
        { "hg2", new List<Piece>() },
        { "hg3", new List<Piece>() },
        { "hy0", new List<Piece>() },
        { "hy1", new List<Piece>() },
        { "hy2", new List<Piece>() },
        { "hy3", new List<Piece>() },
        { "hb0", new List<Piece>() },
        { "hb1", new List<Piece>() },
        { "hb2", new List<Piece>() },
        { "hb3", new List<Piece>() }
    };
            EngineHelper.gameType = gameType;
            EngineHelper.currentPlayerIndex = 0;
            GlobalConstants.MatchMaker.RecievedRequest += new Client.CallbackRecievedRequest(RecievedRequest);

            // Initialize GUI and layout locations
            EngineHelper.InitializeGuiLocations(gui);

            EngineHelper.gui = gui;
            EngineHelper.Glayout = Glayout;
            EngineHelper.Alayout = Alayout;

            // Set rotation based on player color
            int rotation = EngineHelper.SetRotation(playerColor);
            Glayout.RotateTo(rotation);

            // Handle layout size changes
            Alayout.SizeChanged += (sender, e) =>
            {
                Console.WriteLine("The layout has been loaded and rendered.");
                EngineHelper.Pupulate(gui, rotation);
            };

            EngineHelper.InitializePlayers(playerCount, playerColor);
           
            // Initialize original path
            EngineHelper.InitializeOriginalPath();
            if (EngineHelper.replay)
            {
                GameRecorder.engine = this;
                GameRecorder.ReplayGameAsync("GameHistory.json");
            }
            _timer = new Timer(TimerCallback, null, 0, 2000);
            EngineHelper.rolls.Add(6);
            EngineHelper.rolls.Add(4);
            StopTimer();
        }
        public void StopTimer()
        {
            _timer?.Change(Timeout.Infinite, Timeout.Infinite); // Stops the timer
        }
        // Method to start or restart the timer with a 2000 ms interval
        public void StartTimer()
        {
            _timer?.Change(1000, 2000); // Start with an immediate first tick, then every 2000 ms
        }
        private void TimerCallback(object state)
        {
            Console.WriteLine("Tick");
            // Call the PlayGame method on each tick
            if (run)
            {
                run = false;
                StopTimer();
                PlayGame();
            }
        }
        public async void SeatTurn(string seatName)
        {
            Player player = EngineHelper.players[EngineHelper.currentPlayerIndex];
            int tempDice = -1;

            // Check if it's the correct player's turn and if the game is in the roll state
            if (player.Color == seatName && EngineHelper.gameState == "RollDice")
            {
                // Roll the dice
                EngineHelper.diceValue = await EngineHelper.RollDice(seatName);
                tempDice = EngineHelper.diceValue;

                // Determine which pieces can move
                foreach (var piece in player.Pieces)
                {
                    if (piece.Location == 0 && EngineHelper.diceValue == 6)// Open the token if it's in base and dice shows a 6
                        piece.Moveable = true;
                    else if (piece.Location + EngineHelper.diceValue <= 57 && piece.Location != 0)
                        piece.Moveable = true;
                    else
                        piece.Moveable = false;
                }
                List<Piece> moveablePieces = player.Pieces.Where(p => p.Moveable).ToList();

                Console.WriteLine($"{player.Color} rolled a {EngineHelper.diceValue}. Can move {moveablePieces} pieces.");
                GameRecorder.RecordDiceRoll(player, EngineHelper.diceValue);

                // Handle possible scenarios based on the number of moveable pieces
                if (moveablePieces.Count == 1)
                {
                    Console.WriteLine("Turn Animation of the moveable piece;");
                    EngineHelper.gameState = "MovePiece";
                    if (!EngineHelper.replay)
                        await MovePieceAsync(player.Pieces.First(p => p.Moveable).Name); // Move the only moveable piece
                }
                else if (moveablePieces.Count > 0)
                {
                    int firstLocation = moveablePieces[0].Location;
                    EngineHelper.gameState = "MovePiece";
                    if (moveablePieces.All(p => p.Location == firstLocation))
                    {
                        if (!EngineHelper.replay)
                            await MovePieceAsync(moveablePieces.First(p => p.Moveable).Name); // Move the only moveable piece
                    }
                    else
                    {
                        Console.WriteLine("Turn Animation of the moveable pieces;");
                        // Start timer for auto play or prompt for user action
                        if (EngineHelper.gameType == "Computer" && playerColor.ToLower() != EngineHelper.players[EngineHelper.currentPlayerIndex].Color)
                        {
                            if (!EngineHelper.replay)
                                await MovePieceAsync(moveablePieces.First(p => p.Moveable).Name); // Move the only moveable piece
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"{player.Color} could not move any piece.");
                    EngineHelper.ChangeTurn(); // Change turn to the next player
                    EngineHelper.gameState = "RollDice";
                    if (EngineHelper.gameType == "Computer" && playerColor.ToLower() != EngineHelper.players[EngineHelper.currentPlayerIndex].Color)
                    {
                        run = true;
                        StartTimer();
                    }
                }
            }
            else
            {
                Console.WriteLine("Not the turn of the player");
            }

            // Simulate server delay
            await Task.Delay(GlobalConstants.rnd.Next(1, 500));
            StopDice(seatName, tempDice); // Notify the end of the dice roll
        }
        public async Task MovePieceAsync(String pieceName)
        {
            Player player = EngineHelper.players[EngineHelper.currentPlayerIndex];
            Piece piece = EngineHelper.GetPiece(player.Pieces, pieceName);
            if (piece == null || EngineHelper.diceValue == 0)
                return; // Exit if not the current player's piece or no dice roll

            if (EngineHelper.gameState == "MovePiece" && piece.Moveable)
            {
                if (EngineHelper.gameType == "Online")
                    pieceName = await GlobalConstants.MatchMaker.SendMessageAsync(pieceName, "Piece");

                bool killed = false;

                if (piece.Position == -1 && EngineHelper.diceValue == 6) // Moving from base to start
                {
                    piece.Position = player.StartPosition;
                    piece.Location = 1;
                    board[EngineHelper.getPieceBox(piece)].Remove(piece);
                    board[EngineHelper.getPieceBox(piece)].Add(piece);

                    GameRecorder.RecordMove(EngineHelper.diceValue, player, piece, piece.Position, killed); // Prepare animation
                    EngineHelper.Relocate(player, piece, false, 0); // Move to start position
                }
                else if (piece.Location + EngineHelper.diceValue <= 57) // Normal move within bounds
                {
                    int newPosition = (piece.Position + EngineHelper.diceValue) % 52;

                    string pj = EngineHelper.getPieceBox(piece);
                    // Update board and piece positions
                    board[pj].Remove(piece); // Bug fixed I suspect that on move the piece was not being removed for old box
                    piece.Position = newPosition;
                    piece.Location += EngineHelper.diceValue;
                    pj = EngineHelper.getPieceBox(piece);

                    // Check if an opponent’s piece is in the new position
                    if (board[pj].Count != 0 && board[pj].Count < 2 && !EngineHelper.safeZone.Contains(newPosition))
                    {
                        if(board[pj][0].Color != player.Color)
                        {
                            killed = true;
                            board[pj][0].Position = -1; // Send opponent's piece back to base
                            board[pj][0].Location = 0;
                            Piece killedPiece = board[pj][0];
                            board[pj].Remove(killedPiece);
                            board[EngineHelper.getPieceBox(killedPiece)].Add(killedPiece);
                            EngineHelper.Relocate(player, killedPiece, false, 0);
                        }
                    }

                    board[pj].Add(piece);
                    GameRecorder.RecordMove(EngineHelper.diceValue, player, piece, newPosition, killed); // Prepare animation
                    EngineHelper.Relocate(player, piece, false, 0);

                    // Check if piece has reached the end
                    if (piece.Location == 57)
                    {
                        killed = true;
                        player.Pieces.Remove(piece);
                        Console.WriteLine($"{player.Color} piece has reached home!");

                        if (player.Pieces.Count == 0)
                        {
                            Console.WriteLine($"{player.Color} has won the game!");
                            EngineHelper.players.Remove(player);
                        }
                    }
                }
                //checkKills(player,piece);
                EngineHelper.PerformTurnChecks(killed, EngineHelper.diceValue);
                //perform turn turn check
                if (EngineHelper.gameType == "Computer" && playerColor.ToLower() != EngineHelper.players[EngineHelper.currentPlayerIndex].Color)
                {
                    run = true;
                    StartTimer();
                }
            }
        }
        // Record an action for the encoder
        public void PlayGame()
        {
            if (EngineHelper.gameType == "Computer" && playerColor.ToLower() != EngineHelper.players[EngineHelper.currentPlayerIndex].Color)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {

                if (EngineHelper.checkTurn(EngineHelper.players[EngineHelper.currentPlayerIndex].Color, "RollDice"))
                {
                    string SeatName = EngineHelper.players[EngineHelper.currentPlayerIndex].Color;
                        EngineHelper.gui.red.reset();
                        EngineHelper.gui.green.reset();
                        EngineHelper.gui.yellow.reset();
                        EngineHelper.gui.blue.reset();

                        // Handle the dice click for the green player
                        //check turn
                        var seat = EngineHelper.gui.red;
                        if (SeatName == "red")
                            seat = EngineHelper.gui.red;
                        if (SeatName == "green")
                            seat = EngineHelper.gui.green;
                        if (SeatName == "yellow")
                            seat = EngineHelper.gui.yellow;
                        if (SeatName == "blue")
                            seat = EngineHelper.gui.blue;
                        seat.AnimateDice();
                        SeatTurn(SeatName);
                    }
                });
            }
        }
        public void RecievedRequest(String name, int val)
        {
        }
    }
    public static class EngineHelper
    {
        // Game logic helpers
        public static List<int> rolls = new List<int>();
        public static bool replay = !true;

        public static int currentPlayerIndex = 0;
        public static string gameType = "";
        // Game logic helpers
        static private int index = 0;
        public static int diceValue = 0;
        public static string gameState = "RollDice";
        // Private fields
        public static List<Player> players;
        // Public fields
        public static Gui gui;
        // Constants or configuration lists
        private static readonly List<int> home = new List<int> { 52, 11, 24, 37 };
        public static readonly List<int> safeZone = new List<int> { 0, 8, 13, 21, 26, 34, 39, 47, 52, 53, 54, 55, 56, 57, -1 };
        private static Dictionary<string, int[]> originalPath = new Dictionary<string, int[]>();
        // UI Components
        public static AbsoluteLayout Alayout;
        public static Capsule Glayout;
        public static void InitializePlayers(string playerCount, string playerColor)
        {
            // Assume each piece has a UI element or rendering component
            Alayout.Remove(gui.red1);
            Alayout.Remove(gui.red2);
            Alayout.Remove(gui.red3);
            Alayout.Remove(gui.red4);
            Alayout.Remove(gui.gre1);
            Alayout.Remove(gui.gre2);
            Alayout.Remove(gui.gre3);
            Alayout.Remove(gui.gre4);
            Alayout.Remove(gui.yel1);
            Alayout.Remove(gui.yel2);
            Alayout.Remove(gui.yel3);
            Alayout.Remove(gui.yel4);
            Alayout.Remove(gui.blu1);
            Alayout.Remove(gui.blu2);
            Alayout.Remove(gui.blu3);
            Alayout.Remove(gui.blu4);

            int count = int.Parse(playerCount);

            if (playerColor == "Red")
            {
                if (count == 3)
                    players = new List<Player>
                    {
                        new Player("red", gui),
                        new Player("green", gui),
                        new Player("yellow", gui)
                    };
                else if (count == 2)
                    players = new List<Player>
                    {
                        new Player("red", gui),
                        new Player("yellow", gui)
                    };
                else
                    players = new List<Player>
                    {
                        new Player("red", gui),
                        new Player("green", gui),
                        new Player("yellow", gui),
                        new Player("blue", gui)
                    };
            }
            else if (playerColor == "Green")
            {
                if (count == 3)
                    players = new List<Player>
                    {
                        new Player("green", gui),
                        new Player("yellow", gui),
                        new Player("blue", gui)
                    };
                else if (count == 2)
                    players = new List<Player>
                    {
                        new Player("green", gui),
                        new Player("blue", gui)
                    };
                else
                    players = new List<Player>
                    {
                        new Player("green", gui),
                        new Player("yellow", gui),
                        new Player("blue", gui),
                    new Player("red", gui)
                    };
            }
            else if (playerColor == "Yellow")
            {
                if (count == 3)
                    players = new List<Player>
                    {
                        new Player("yellow", gui),
                        new Player("blue", gui),
                        new Player("red", gui)
                    };
                else if (count == 2)
                    players = new List<Player>
                    {
                        new Player("yellow", gui),
                        new Player("red", gui)
                    };
                else
                    players = new List<Player>
                    {
                        new Player("yellow", gui),
                        new Player("blue", gui),
                        new Player("red", gui),
                        new Player("green", gui)
                    };
            }
            else if (playerColor == "Blue")
            {
                if (count == 3)
                    players = new List<Player>
                    {
                        new Player("blue", gui),
                        new Player("red", gui),
                        new Player("green", gui)
                    };
                else if (count == 2)
                    players = new List<Player>
                    {
                        new Player("blue", gui),
                        new Player("green", gui)
                    };
                else
                    players = new List<Player>
                    {
                        new Player("blue", gui),
                        new Player("red", gui),
                        new Player("green", gui),
                        new Player("yellow", gui)
                    };
            }
            else
            {
                throw new ArgumentException("Invalid player color selected.");
            }
        }
        public static void InitializeGuiLocations(Gui gui)
        {
            // Set initial locations for each token to -1
            foreach (var token in new[] { gui.red1, gui.red2, gui.red3, gui.red4,
                                   gui.gre1, gui.gre2, gui.gre3, gui.gre4,
                                   gui.blu1, gui.blu2, gui.blu3, gui.blu4,
                                   gui.yel1, gui.yel2, gui.yel3, gui.yel4 })
            {
                token.location = -1;
            }
        }
        public static int SetRotation(string playerColor)
        {
            return playerColor switch
            {
                "Green" => 270,
                "Yellow" => 180,
                "Blue" => 90,
                _ => 360 // Default rotation for Red or unrecognized color
            };
        }
        public static void InitializeOriginalPath()
        {
            originalPath = new Dictionary<string, int[]>
            {
                { "p0", new int[] { 13, 6 } },
                { "p1", new int[] { 12, 6 } },
                { "p2", new int[] { 11, 6 } },
                { "p3", new int[] { 10, 6 } },
                { "p4", new int[] { 9, 6 } },
                { "p5", new int[] { 8, 5 } },
                { "p6", new int[] { 8, 4 } },
                { "p7", new int[] { 8, 3 } },
                { "p8", new int[] { 8, 2 } },
                { "p9", new int[] { 8, 1 } },
                { "p10", new int[] { 8, 0 } },
                { "p11", new int[] { 7, 0 } },
                { "p12", new int[] { 6, 0 } },
                { "p13", new int[] { 6, 1 } },
                { "p14", new int[] { 6, 2 } },
                { "p15", new int[] { 6, 3 } },
                { "p16", new int[] { 6, 4 } },
                { "p17", new int[] { 6, 5 } },
                { "p18", new int[] { 5, 6 } },
                { "p19", new int[] { 4, 6 } },
                { "p20", new int[] { 3, 6 } },
                { "p21", new int[] { 2, 6 } },
                { "p22", new int[] { 1, 6 } },
                { "p23", new int[] { 0, 6 } },
                { "p24", new int[] { 0, 7 } },
                { "p25", new int[] { 0, 8 } },
                { "p26", new int[] { 1, 8 } },
                { "p27", new int[] { 2, 8 } },
                { "p28", new int[] { 3, 8 } },
                { "p29", new int[] { 4, 8 } },
                { "p30", new int[] { 5, 8 } },
                { "p31", new int[] { 6, 9 } },
                { "p32", new int[] { 6, 10 } },
                { "p33", new int[] { 6, 11 } },
                { "p34", new int[] { 6, 12 } },
                { "p35", new int[] { 6, 13 } },
                { "p36", new int[] { 6, 14 } },
                { "p37", new int[] { 7, 14 } },
                { "p38", new int[] { 8, 14 } },
                { "p39", new int[] { 8, 13 } },
                { "p40", new int[] { 8, 12 } },
                { "p41", new int[] { 8, 11 } },
                { "p42", new int[] { 8, 10 } },
                { "p43", new int[] { 8, 9 } },
                { "p44", new int[] { 9, 8 } },
                { "p45", new int[] { 10, 8 } },
                { "p46", new int[] { 11, 8 } },
                { "p47", new int[] { 12, 8 } },
                { "p48", new int[] { 13, 8 } },
                { "p49", new int[] { 14, 8 } },
                { "p50", new int[] { 14, 7 } },
                { "p51", new int[] { 14, 6 } },
                { "r51", new int[] { 13, 7 } },
                { "r52", new int[] { 12, 7 } },
                { "r53", new int[] { 11, 7 } },
                { "r54", new int[] { 10, 7 } },
                { "r55", new int[] { 9, 7 } },
                { "r56", new int[] { 8, 7 } },
                { "g51", new int[] { 7, 1 } },
                { "g52", new int[] { 7, 2 } },
                { "g53", new int[] { 7, 3 } },
                { "g54", new int[] { 7, 4 } },
                { "g55", new int[] { 7, 5 } },
                { "g56", new int[] { 7, 6 } },
                { "y51", new int[] { 1, 7 } },
                { "y52", new int[] { 2, 7 } },
                { "y53", new int[] { 3, 7 } },
                { "y54", new int[] { 4, 7 } },
                { "y55", new int[] { 5, 7 } },
                { "y56", new int[] { 6, 7 } },
                { "b51", new int[] { 7, 13 } },
                { "b52", new int[] { 7, 12 } },
                { "b53", new int[] { 7, 11 } },
                { "b54", new int[] { 7, 10 } },
                { "b55", new int[] { 7, 9 } },
                { "b56", new int[] { 7, 8 } },
                { "hr0", new int[] { 11, 2 } },
                { "hr1", new int[] { 11, 3 } },
                { "hr2", new int[] { 12, 2 } },
                { "hr3", new int[] { 12, 3 } },
                { "hg0", new int[] { 2, 2 } },
                { "hg1", new int[] { 2, 3 } },
                { "hg2", new int[] { 3, 2 } },
                { "hg3", new int[] { 3, 3 } },
                { "hy0", new int[] { 2, 11 } },
                { "hy1", new int[] { 2, 12 } },
                { "hy2", new int[] { 3, 11 } },
                { "hy3", new int[] { 3, 12 } },
                { "hb0", new int[] { 11, 11 } },
                { "hb1", new int[] { 11, 12 } },
                { "hb2", new int[] { 12, 11 } },
                { "hb3", new int[] { 12, 12 } }
            };
        }
        public static string getPieceBox(Piece piece)
        {
            //piece.Position
            //player.StartPosition
            string pj = piece.Position == -1
                    ? "h" + piece.Name.Substring(0, 1) + (int.Parse(piece.Name.Substring(3, 1)) - 1)
                    : "p" + piece.Position;

            if (piece.Location > 51 && piece.Location < 58)
            {
                pj = piece.Name.Substring(0, 1) + (piece.Location - 1);
            }
            return pj;
        }
        public static void Relocate(Player player, Piece piece, bool baseflag, int rotation)
        {
            //piece.Position
            //player.StartPosition
            string pj = getPieceBox(piece);

            if (piece.Location <= 57)
            {
                double width = Alayout.Width / 15;
                double height = Alayout.Height / 15;
                double x = originalPath[pj][1] * width;
                double y = originalPath[pj][0] * height;

                if (baseflag)
                {
                    AbsoluteLayout.SetLayoutBounds(piece.PieceToken, new Rect(0, 0, width, height));
                    // AbsoluteLayout.SetLayoutBounds(piece.piece, new Rect(y, x, width, height));
                    // AbsoluteLayout.SetLayoutBounds(piece.piece, new Rect(x, y, width, height));
                    piece.PieceToken.RotateTo(-rotation);
                }

                piece.PieceToken.TranslateTo(x, y, 200, Easing.CubicIn);
                // Grid.SetRow(piece.piece, originalPath[pj][0]);
                // Grid.SetRow(piece.piece, originalPath[pj][0]);
                // Grid.SetColumn(piece.piece, originalPath[pj][1]);
                Console.WriteLine($"{piece.Name} is at {pj} - Position: ({x}, {y}), Size: ({width}, {height})");
            }
            else
            {

            }
        }
        public static void Pupulate(Gui gui, int rotation)
        {
            //players[0].Pieces[0].location = 50;
            //players[0].Pieces[0].Position = 49;
            for (int i = 0; i < players.Count; i++)
            {
                for (int j = 0; j < players[i].Pieces.Count; j++)
                {
                    Relocate(players[i], players[i].Pieces[j], true, rotation);
                }
            }
        }
        private static bool IsPieceSafe(Player player, Piece piece)
        {
            return safeZone.Contains(piece.Position);
        }
        public static void PerformTurnChecks(bool killed, int diceValue = -1)
        {
            gameState = "RollDice";

            if (!killed)
            {
                if (diceValue != 6)
                {
                    ChangeTurn();
                }
                else
                {
                    // Auto Play logic here if enabled
                    // Optionally move the piece automatically and then change the turn
                }
            }
            diceValue = 0;  // Reset dice value for the next turn
        }
        public static bool checkTurn(String SeatName, String GameState)
        {
            Player player = players[currentPlayerIndex];
            if (player.Color == SeatName && EngineHelper.gameState == GameState)
            {
                return true;
            }
            else
                return false;
        }
        public static async Task<int> RollDice(string seatName = "")
        {
            if(replay)
            {
                return GameRecorder.RequestDice();
            }
            if (rolls.Count != 0)
            {
                diceValue = rolls[0];
                rolls.RemoveAt(0);
                return diceValue;
            }
                if (gameType != "Online")
                {
                    return GlobalConstants.rnd.Next(1, 7);
                }
                else
                    return Int32.Parse(await GlobalConstants.MatchMaker.SendMessageAsync(seatName, "Seat"));
        }
        public static void ChangeTurn()
        {
            currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
            Player currentPlayer = players[currentPlayerIndex];

            foreach (var piece in currentPlayer.Pieces)
            {
                // Safely update the UI
                Alayout.Remove(piece.PieceToken);
                Alayout.Add(piece.PieceToken);
            }
        }
        public static Piece GetPiece(List<Piece> pieces, string name)
        {
            foreach (var piece in pieces)
            {
                if (piece.Moveable && piece.Name == name)
                {
                    // If the piece is at start (-1) and dice roll is not 6, it's not eligible for selection
                    if (piece.Position == -1 && diceValue != 6)
                    {
                        return null;
                    }
                    return piece;
                }
            }
            return null;
        }
        public static bool IsGameOver()
        {
            foreach (Player player in players)
            {
                if (player.Pieces.Count == 0)
                {
                    Console.WriteLine($"{player.Color} has won the game!");
                    return true;
                }
            }
            return false;
        }
    }
}