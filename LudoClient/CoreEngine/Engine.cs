
using LudoClient.Constants;
using LudoClient.ControlView;
using LudoClient.Network;
using Microsoft.Maui.Storage;

namespace LudoClient.CoreEngine
{
    public class Engine
    {
        // UI Components
        private AbsoluteLayout Alayout;
        private Capsule Glayout;

        // Public fields
        public Gui gui;

        // Events
        public delegate void CallbackEventHandler(string SeatName, int diceValue);
        public event CallbackEventHandler StopDice;

        // Private fields
        private List<Player> players;
        private int currentPlayerIndex = 0;
        private Piece[] board = new Piece[57];
        private string gameType = "";
        private int diceValue = 0;
        private string gameState = "RollDice";

        // Constants or configuration lists
        private readonly List<int> home = new List<int> { 52, 11, 24, 37 };
        private readonly List<int> safeZone = new List<int> { 0, 8, 13, 21, 26, 34, 39, 47, 52, 53, 54, 55, 56, 57, -1 };
        private          Dictionary<string, int[]> originalPath = new Dictionary<string, int[]>();

        // Game logic helpers
        bool replay = true;
        private int index = 0;
        private void InitializeGuiLocations(Gui gui)
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
        private int SetRotation(string playerColor)
        {
            return playerColor switch
            {
                "Green" => 270,
                "Yellow" => 180,
                "Blue" => 90,
                _ => 360 // Default rotation for Red or unrecognized color
            };
        }
        private void InitializeOriginalPath()
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
        public Engine(string gameType, string playerCount, string playerColor, Gui gui, Capsule Glayout, AbsoluteLayout Alayout)
        {
            this.gameType = gameType;
            currentPlayerIndex = 0;
            GlobalConstants.MatchMaker.RecievedRequest += new Client.CallbackRecievedRequest(RecievedRequest);

            // Initialize GUI and layout locations
            InitializeGuiLocations(gui);

            this.gui = gui;
            this.Glayout = Glayout;
            this.Alayout = Alayout;

            // Set rotation based on player color
            int rotation = SetRotation(playerColor);
            Glayout.RotateTo(rotation);

            // Handle layout size changes
            Alayout.SizeChanged += (sender, e) =>
            {
                Console.WriteLine("The layout has been loaded and rendered.");
                Pupulate(gui, rotation);
            };
            // Initialize players
            players = new List<Player>
            {
                new Player("red", gui),
                new Player("green", gui),
                new Player("yellow", gui),
                new Player("blue", gui)
            };
            // Initialize original path
            InitializeOriginalPath();
            GameRecorder.engine = this;
            GameRecorder.ReplayGameAsync("GameHistory.json");
        }
        private Piece GetPiece(List<Piece> pieces, string name,int diceValue=0)
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
        private void PerformTurnChecks(bool killed, int diceValue = -1)
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
        private bool IsPieceSafe(Player player, Piece piece)
        {
            return safeZone.Contains(piece.Position);
        }
        public bool checkTurn(String SeatName, String GameState)
        {
            Player player = players[currentPlayerIndex];
            if (player.Color == SeatName && gameState == GameState)
            {
                return true;
            }
            else
                return false;
        }
        public async void SeatTurn(string seatName,int diceValue=0)
        {
            Player player = players[currentPlayerIndex];
            int tempDice = -1;

            // Check if it's the correct player's turn and if the game is in the roll state
            if (player.Color == seatName && gameState == "RollDice")
            {
                int moveablePieces = 0;
                int closedPieces = 0;

                // Roll the dice
                diceValue = await RollDice(seatName, diceValue);
                tempDice = diceValue;

                // Determine which pieces can move
                foreach (var piece in player.Pieces)
                {
                    if (piece.Location == 0 && diceValue == 6)
                    {
                        // Open the token if it's in base and dice shows a 6
                        piece.Moveable = true;
                        moveablePieces++;
                        closedPieces++;
                    }
                    else if (piece.Location + diceValue <= 57 && piece.Location != 0)
                    {
                        piece.Moveable = true;
                        moveablePieces++;
                    }
                    else
                    {
                        piece.Moveable = false;
                    }
                }

                Console.WriteLine($"{player.Color} rolled a {diceValue}. Can move {moveablePieces} pieces.");
                GameRecorder.RecordDiceRoll(player, diceValue);

                // Handle possible scenarios based on the number of moveable pieces
                if (moveablePieces == 1)
                {
                    Console.WriteLine("Turn Animation of the moveable piece;");
                    gameState = "MovePiece";
                    if (!replay)
                        await MovePieceAsync(player.Pieces.First(p => p.Moveable).Name); // Move the only moveable piece
                }
                else if (moveablePieces == player.Pieces.Count && diceValue == 6 && closedPieces == player.Pieces.Count)
                {
                    gameState = "MovePiece";
                    if (!replay)
                        await MovePieceAsync(player.Pieces[GlobalConstants.rnd.Next(0, player.Pieces.Count)].Name); // Randomly move one piece
                }
                else if (moveablePieces > 0)
                {
                    Console.WriteLine("Turn Animation of the moveable pieces;");
                    // Start timer for auto play or prompt for user action
                    gameState = "MovePiece";
                }
                else
                {
                    Console.WriteLine($"{player.Color} could not move any piece.");
                    ChangeTurn(); // Change turn to the next player
                    gameState = "RollDice";
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
        public async Task<int> RollDice(string seatName="", int diceValue=0)
        {
            if (diceValue == 0)
            {
                if (gameType != "Online")
                    return GlobalConstants.rnd.Next(1, 7);
                else
                    return Int32.Parse(await GlobalConstants.MatchMaker.SendMessageAsync(seatName, "Seat"));
            }else return diceValue;
        }
        public async Task MovePieceAsync(String pieceName, int diceValue = -1)
        {
            Player player = players[currentPlayerIndex];
            Piece piece = GetPiece(player.Pieces, pieceName, diceValue);
            if (piece == null || diceValue == 0)
                return; // Exit if not the current player's piece or no dice roll

            if (gameState == "MovePiece" && piece.Moveable)
            {
                if (gameType == "Online")
                    pieceName = await GlobalConstants.MatchMaker.SendMessageAsync(pieceName, "Piece");

                bool killed = false;

                if (piece.Position == -1 && diceValue == 6) // Moving from base to start
                {
                    piece.Position = player.StartPosition;
                    piece.Location = 1;
                    board[player.StartPosition] = piece;

                    GameRecorder.RecordMove(diceValue, player, piece, piece.Position, killed); // Prepare animation
                    Relocate(player, piece, false, 0); // Move to start position
                }
                else if (piece.Location + diceValue <= 57) // Normal move within bounds
                {
                    int newPosition = (piece.Position + diceValue) % 52;

                    // Check if an opponent’s piece is in the new position
                    if (board[newPosition] != null && board[newPosition].Color != player.Color)
                    {
                        killed = true;
                        board[newPosition].Position = -1; // Send opponent's piece back to base
                        board[newPosition].Location = 0;
                        Relocate(player, board[newPosition], false, 0);
                    }

                    // Update board and piece positions
                    board[piece.Position] = null;
                    piece.Position = newPosition;
                    piece.Location += diceValue;
                    board[newPosition] = piece;

                    GameRecorder.RecordMove(diceValue, player, piece, newPosition, killed); // Prepare animation
                    Relocate(player, piece, false, 0);

                    // Check if piece has reached the end
                    if (piece.Location == 57)
                    {
                        player.Pieces.Remove(piece);
                        Console.WriteLine($"{player.Color} piece has reached home!");

                        if (player.Pieces.Count == 0)
                            Console.WriteLine($"{player.Color} has won the game!");
                    }
                }
                //checkKills(player,piece);
                PerformTurnChecks(killed, diceValue);
                //perform turn turn check
            }
        }
        // Record an action for the encoder
       
        private void ChangeTurn()
        {
            currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        }
        private bool IsGameOver()
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
        public void PlayGame()
        {
            SeatTurn(players[currentPlayerIndex].Color);
        }
        public void Relocate(Player player, Piece piece, bool baseflag, int rotation)
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
        public void Pupulate(Gui gui, int rotation)
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
        public void RecievedRequest(String name, int val)
        {
        }
        // Method to load and replay a saved game
        
    }
}