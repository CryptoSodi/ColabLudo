using SharedCode.Constants;
using System.IO.Pipelines;

namespace SharedCode.CoreEngine
{
    public class Engine
    {
        public static GameRecorder gameRecorder;
        static string PlayState = "Active";
        // Events
        public delegate void CallbackEventHandler(string SeatName, int diceValue);
        public event CallbackEventHandler StopDice;

        public delegate void Callback_AnimateDice_EventHandler(string SeatName);
        public event Callback_AnimateDice_EventHandler AnimateDice;

        public delegate Task CallbackEventHandlerRelocateAsync(Piece piece, Piece pieceClone);
        public event CallbackEventHandlerRelocateAsync RelocateAsync;

        public delegate void CallbackEventHandlerStartProgressAnimation(string SeatColor);
        public event CallbackEventHandlerStartProgressAnimation StartProgressAnimation;

        public delegate void CallbackEventHandlerStopProgressAnimation(string SeatColor);
        public event CallbackEventHandlerStopProgressAnimation StopProgressAnimation;

        public delegate Task CallbackEventHandlerShowResults(string SeatColor, string GameType, string GameCost);
        public event CallbackEventHandlerShowResults ShowResults;

        public delegate void CallbackEventHandlerPlayerLeft(string SeatColor, bool SendToServer = true);
        public event CallbackEventHandlerPlayerLeft PlayerLeftSeat;
        // Game logic helpers
        public static Dictionary<string, List<Piece>>? board;

        public EngineHelper EngineHelper = new EngineHelper();
        public async Task<string> TimerTimeoutAsync(String SeatName)
        {
            string result = "";
           switch (EngineHelper.gameState){
                case "RollDice":
                    result = await SeatTurn(SeatName, "", "");
                    return result;
                case "MovePiece":
                    Player player = EngineHelper.currentPlayer;
                    List<Piece> moveablePieces = player.Pieces.Where(p => p.Moveable).ToList();
                    
                    await MovePieceAsync(moveablePieces[GlobalConstants.rnd.Next(0,moveablePieces.Count)].Name);
                    result = moveablePieces[GlobalConstants.rnd.Next(0, moveablePieces.Count)].Name;
                    return result;
            }
            return "";
        }
        public Engine(string gameMode, string gameType, string playerCount, string playerColor, string rollsString="")
        {
            gameRecorder = new GameRecorder(this);
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
            EngineHelper.gameMode = gameMode;

            EngineHelper.InitializePlayers(playerCount, playerColor);

            // Initialize original path
            EngineHelper.InitializeOriginalPath();
            if (EngineHelper.replay)
            {
                gameRecorder.engine = this;
                _ = gameRecorder.ReplayGameAsync("GameHistory.json");
            }

            EngineHelper.currentPlayer = EngineHelper.players[0];

            PlayState = "Active";
            if (EngineHelper.stopAnimate)
                TimerTimeoutAsync(EngineHelper.currentPlayer.Color);

            if (gameMode == "Server")
                for (int i = 0; i < 5000; i++)
                    EngineHelper.rolls.Add(GlobalConstants.rnd.Next(1, 7));
            if (gameMode == "Client")
                EngineHelper.rolls = rollsString.Select(c => int.Parse(c.ToString())).ToList();

            EngineHelper.rollsString = string.Join("", EngineHelper.rolls);
        }
        public async Task<string> SeatTurn(string seatName, String DiceValue, String Piece, bool SendToServer=true)
        {
            if (PlayState == "Stop")
                return "";

            int tempDice = -1;
            string tempPiece = "";
            // Check if it's the correct player's turn and if the game is in the roll state
            if (EngineHelper.checkTurn(seatName, "RollDice"))
            {
                EngineHelper.gameState = "RollingDice";
                if (AnimateDice!=null)
                    AnimateDice(seatName);

                int localDiceChecker = -1;
                if (EngineHelper.gameMode == "Client")
                {
                    localDiceChecker = EngineHelper.rolls[0];
                    EngineHelper.rolls.RemoveAt(0);
                    _ = Task.Run(async () =>
                    {
                        // Notify the end of the dice roll
                        if (StopDice != null && EngineHelper.rolls.Count > 0)
                        {
                            await Task.Delay(100);
                            StopDice(seatName, localDiceChecker);
                        }
                    });
                }
                // Roll the dice
                if (DiceValue != "")
                    EngineHelper.diceValue = Int32.Parse(DiceValue);
                else
                {
                    String DiceServerValueHolder = await EngineHelper.RollDice(seatName);
                    EngineHelper.diceValue = Int32.Parse(DiceServerValueHolder.Split(",")[0]);
                    Piece = DiceServerValueHolder.Split(",")[1];
                }
                
                tempDice = EngineHelper.diceValue;

                if (EngineHelper.gameMode != "Client" && EngineHelper.gameMode != "Server")
                {
                    if (!EngineHelper.stopAnimate)
                        // Simulate server delay
                        await Task.Delay(200);
                    else
                        await Task.Delay(30);
                    // Notify the end of the dice roll
                }

                if(EngineHelper.gameMode == "Client" && localDiceChecker != EngineHelper.diceValue)
                {
                    Console.WriteLine("ERROR SERVER OUT OF SYNC");
                }

                if (StopDice!=null && EngineHelper.gameMode != "Client")
                    StopDice(seatName, tempDice);

                // Determine which pieces can move
                foreach (var piece in EngineHelper.currentPlayer.Pieces)
                {
                    //piece.Moveable = (piece.Location == 0 && EngineHelper.diceValue == 6) ||
                    // (piece.Location != 0 && piece.Location + EngineHelper.diceValue <= 57);
                    if (piece.Location == 0 && EngineHelper.diceValue == 6)// Open the token if it's in base and dice shows a 6
                        piece.Moveable = true;
                    else if (piece.Location + EngineHelper.diceValue <= 57 && piece.Location != 0)
                        piece.Moveable = true;
                    else
                        piece.Moveable = false;
                }

                List<Piece> moveablePieces = EngineHelper.currentPlayer.Pieces.Where(p => p.Moveable).ToList();
                Console.WriteLine($"{EngineHelper.currentPlayer.Color} rolled a {EngineHelper.diceValue}. Can move {moveablePieces.Count} pieces.");

                gameRecorder.RecordDiceRoll(EngineHelper.currentPlayer, EngineHelper.diceValue);

                // Handle possible scenarios based on the number of moveable pieces
                bool moveSeat = false;

                if (moveablePieces.Count == 1)
                {
                    Console.WriteLine("Turn Animation of the moveable piece;");
                    moveSeat = true;
                }
                else if (moveablePieces.Count > 0)
                {
                    EngineHelper.gameState = "MovePiece";
                    int firstLocation = moveablePieces[0].Location;
                    if (moveablePieces.All(p => p.Location == firstLocation))
                        moveSeat = true;
                    else
                    {
                        Console.WriteLine("Turn Animation of the moveable pieces;");
                        if (!EngineHelper.stopAnimate)
                            // Start timer for auto play or prompt for user action
                            StartProgressAnimation(EngineHelper.currentPlayer.Color);
                        else
                        {
                            Console.WriteLine("PREVENT ANIMATION TIER");
                        }
                    }
                }
                else
                {
                    EngineHelper.animationBlock = false;
                    Console.WriteLine($"{EngineHelper.currentPlayer.Color} could not move any piece.");
                    if(StopProgressAnimation!=null)
                        StopProgressAnimation(EngineHelper.currentPlayer.Color);
                    EngineHelper.ChangeTurn(); // Change turn to the next player
                    if (!EngineHelper.stopAnimate && StartProgressAnimation!=null)
                        StartProgressAnimation(EngineHelper.currentPlayer.Color);
                    EngineHelper.gameState = "RollDice";
                }

                if (moveSeat)
                {
                    EngineHelper.gameState = "MovePiece";

                    if (!EngineHelper.replay)
                    {
                        if (Piece != "")
                            tempPiece = Piece;
                        else
                            tempPiece = moveablePieces.First(p => p.Moveable).Name;
                        tempPiece = await MovePieceAsync(tempPiece, false);       // Move the only moveable piece
                    }
                }
                else
                {
                    //TimerTimeout(EngineHelper.currentPlayer.Color);
                }
            }
            else
            {
                Console.WriteLine("Not the turn of the player");
            }
            return $"{tempDice},{tempPiece}";
        }
        public async Task<string> MovePieceAsync(String pieceName, bool SendToServer=true)
        {
            String tempPiece = "";
            if (PlayState == "Stop")
                return "";
            Player player = EngineHelper.currentPlayer;
            Piece piece = EngineHelper.GetPiece(player.Pieces, pieceName);
            if (piece == null || EngineHelper.diceValue == 0)
                return ""; // Exit if not the current player's piece or no dice roll

            if (piece.Moveable && EngineHelper.checkTurn(pieceName, "MovePiece"))
            {
                String ServerpieceName = pieceName;
                EngineHelper.gameState = "MovingPiece";
                if (EngineHelper.gameMode == "Client" && SendToServer)
                {
                    GlobalConstants.MatchMaker?.SendMessageAsync(pieceName, "MovePiece").ContinueWith(t =>
                        {
                            if (t.Status == TaskStatus.RanToCompletion)
                            {
                                ServerpieceName = t.Result;
                                if (EngineHelper.gameMode == "Client" && ServerpieceName != pieceName)
                                {
                                    Console.WriteLine("ERROR SERVER OUT OF SYNC AT PIECE");
                                }
                            }
                            else
                            {
                                ServerpieceName = "Error"; // Handle failure
                            }
                        });
                }

                
                
                pieceName = ServerpieceName;
                bool killed = false;
                Piece pieceClone = piece.Clone();

                if (piece.Position == -1 && EngineHelper.diceValue == 6) // Moving from base to start
                {
                    piece.Jump(this, EngineHelper.diceValue);
                    tempPiece = pieceName;
                    if (RelocateAsync != null)
                        RelocateAsync(piece, piece.Clone());
                    gameRecorder.RecordMove(EngineHelper.diceValue, player, piece, piece.Position, killed);
                }
                else if (piece.Location + EngineHelper.diceValue <= 57) // Normal move within bounds
                {
                    piece.Jump(this, EngineHelper.diceValue);
                    tempPiece = pieceName;
                    string pj = EngineHelper.getPieceBox(piece);
                   // List<Piece> kilablePieces = board[pj].Where(p => p.Color != piece.Color).ToList();
                    List<Piece> kilablePieces = board[pj].Where(p => p.Color != piece.Color && !(EngineHelper.gameType == "22" && EngineHelper.IsTeammate(piece.Color, p.Color))).ToList();

                    // Prevent killing if there are two or more opponent pieces
                    if (kilablePieces.Count == 1 && !EngineHelper.safeZone.Contains(piece.Position))
                    {
                        killed = true;
                        Piece killedPiece = kilablePieces[0];
                        killedPiece.Position = -1; // Send opponent's piece back to base
                        killedPiece.Location = 0;
                        board[pj].Remove(killedPiece);
                        board[EngineHelper.getPieceBox(killedPiece)].Add(killedPiece);
                    }

                    if (RelocateAsync != null)
                        await RelocateAsync(piece, pieceClone);
                    if (killed && RelocateAsync != null)
                        await RelocateAsync(kilablePieces[0], kilablePieces[0]);

                    gameRecorder.RecordMove(EngineHelper.diceValue, player, piece, piece.Position, killed); // Prepare animation
                    if (piece.Location == 57)
                    {
                        killed = true;
                        player.Pieces.Remove(piece);
                        Console.WriteLine($"{player.Color} piece has reached home!");

                        if (player.Pieces.Count == 0)
                        {
                            EngineHelper.diceValue = 0;
                            killed = false;
                        }
                    }
                }
                else
                {
                    EngineHelper.animationBlock = false;
                }

                StopProgressAnimation(EngineHelper.currentPlayer.Color);
                //checkKills(player,piece);
                EngineHelper.PerformTurnChecks(killed, EngineHelper.diceValue);

                if (!EngineHelper.stopAnimate)
                    StartProgressAnimation(EngineHelper.currentPlayer.Color);

                // Check if piece has reached the end
                if (player.Pieces.Count == 0)
                {
                    player.playState = "Home";
                    Console.WriteLine($"{player.Color} has won the game!");
                    // EngineHelper.players.Remove(player);
                    List<Player> winners = EngineHelper.checkGameOver();
                    if (winners.Count > 0)
                    { 
                        //GANE OVER
                        GameOver(winners);
                        return tempPiece;
                    }
                }

                if (!EngineHelper.stopAnimate)
                    //perform turn turn check
                    StartProgressAnimation(EngineHelper.currentPlayer.Color);
                else
                    TimerTimeoutAsync(EngineHelper.currentPlayer.Color);
            }
            else 
                return "";
            return tempPiece;
        }

        

        public void PlayerLeft(String playerColor, bool SendToServer = true)
        {
            Player player = EngineHelper.getPlayer(playerColor);
            player.playState = "Left";
            //EngineHelper.players.RemoveAll(p => p.Color == playerColor);
            if (EngineHelper.currentPlayer.Color == playerColor)
                EngineHelper.ChangeTurn();
            if (PlayerLeftSeat != null)
                PlayerLeftSeat(playerColor);
            List<Player> winners = EngineHelper.checkGameOver();
            if (winners.Count>0)
            {
                //GANE OVER
                GameOver(winners);
            }
        }
        private void GameOver(List<Player> winners)
        {
            if (PlayState == "Active" && EngineHelper.gameMode != "Client" && ShowResults != null)
            {
                string GameType = EngineHelper.gameType;
                //, string GameCost
                cleanGame();
                // Show game over dialog if the game is not in online mode
                if (GameType == "22")
                    ShowResults(winners[0].Color + "," + winners[1].Color, GameType, "0");
                else
                    ShowResults(winners[0].Color + ",", GameType, "0");
            }
#if WINDOWS
            gameRecorder.SaveGameHistory();
#endif
        }
        public void cleanGame()
        {
            StopProgressAnimation("red");
            StopProgressAnimation("green");
            StopProgressAnimation("yellow");
            StopProgressAnimation("blue");

            PlayState = "Stop";
            EngineHelper.players.Clear();
            EngineHelper.rolls.Clear();
            EngineHelper.gameType = "";
            EngineHelper.gameMode = "";
            EngineHelper.gameState = "RollDice";
        }
    }
    public class EngineHelper
    {
        // Game logic helpers
        public List<int> rolls = new List<int>();
        public string rollsString;
        public bool replay = !true;
        public bool stopAnimate = !true;
        public Player currentPlayer = null;
        public string gameType = "";
        public string gameMode = "";
        // Game logic helpers
        public int diceValue = 0;
        public string gameState = "RollDice";
        // Private fields
        public List<Player> players;
        // Public fields
        // Constants or configuration lists
        public readonly List<int> safeZone = new List<int> {0, 8, 13, 21, 26, 34, 39, 47, 52, 53, 54, 55, 56, 57, -1};
        public Dictionary<string, int[]> originalPath = new Dictionary<string, int[]>();
       
        public bool animationBlock = false;

        public Player getPlayer(String color)
        {
            return players.FirstOrDefault(p => p.Color == color);
        }
        public void InitializePlayers(string playerCount, string playerColor)
        {
            // Assume each piece has a UI element or rendering component
            int count = int.Parse(playerCount);
            if (playerColor == "Red")
            {
                if (count == 3)
                    players = new List<Player>
                    {
                        new Player("red"),
                        new Player("green"),
                        new Player("yellow")
                    };
                else if (count == 2)
                    players = new List<Player>
                    {
                        new Player("red"),
                        new Player("yellow")
                    };
                else
                    players = new List<Player>
                    {
                        new Player("red"),
                        new Player("green"),
                        new Player("yellow"),
                        new Player("blue")
                    };
            }
            else if (playerColor == "Green")
            {
                if (count == 3)
                    players = new List<Player>
                    {
                        new Player("green"),
                        new Player("yellow"),
                        new Player("blue")
                    };
                else if (count == 2)
                    players = new List<Player>
                    {
                        new Player("green"),
                        new Player("blue")
                    };
                else
                    players = new List<Player>
                    {
                        new Player("green"),
                        new Player("yellow"),
                        new Player("blue"),
                        new Player("red")
                    };
            }
            else if (playerColor == "Yellow")
            {
                if (count == 3)
                    players = new List<Player>
                    {
                        new Player("yellow"),
                        new Player("blue"),
                        new Player("red")
                    };
                else if (count == 2)
                    players = new List<Player>
                    {
                        new Player("yellow"),
                        new Player("red")
                    };
                else
                    players = new List<Player>
                    {
                        new Player("yellow"),
                        new Player("blue"),
                        new Player("red"),
                        new Player("green")
                    };
            }
            else if (playerColor == "Blue")
            {
                if (count == 3)
                    players = new List<Player>
                    {
                        new Player("blue"),
                        new Player("red"),
                        new Player("green")
                    };
                else if (count == 2)
                    players = new List<Player>
                    {
                        new Player("blue"),
                        new Player("green")
                    };
                else
                    players = new List<Player>
                    {
                        new Player("blue"),
                        new Player("red"),
                        new Player("green"),
                        new Player("yellow")
                    };
            }
            else
            {
                throw new ArgumentException("Invalid player color selected.");
            }
        }
        public int SetRotation(string playerColor)
        {
            return playerColor switch
            {
                "Green" => 270,
                "Yellow" => 180,
                "Blue" => 90,
                _ => 360 // Default rotation for Red or unrecognized color
            };
        }
        public void InitializeOriginalPath()
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
        public string getPieceBox(Piece piece)
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
        public void PerformTurnChecks(bool killed, int diceValue = -1)
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
        public bool checkTurn(String SeatNameOrPiece, String GameState)
        {
            return ((currentPlayer.Color == SeatNameOrPiece || currentPlayer.Color.ToLower().Contains(SeatNameOrPiece.Replace("1", "").Replace("2", "").Replace("3", "").Replace("4", ""))) && gameState == GameState);
        }
        public async Task<String> RollDice(string seatName = "")
        {
            if(replay)
            {
                return Engine.gameRecorder.RequestDice()+"";
            }
            if (gameMode == "Client")
                return await GlobalConstants.MatchMaker?.SendMessageAsync(seatName, "DiceRoll");
            else
            if (rolls.Count != 0)
            {
                diceValue = rolls[0];
                rolls.RemoveAt(0);
                return diceValue+",";
            }
            else
            {
                return GlobalConstants.rnd.Next(1, 7) + ",";
            }
        }
        public void ChangeTurn(int retry = 0)
        {
            if (retry >= players.Count)
                return;

            currentPlayer = players[(players.IndexOf(currentPlayer) + 1) % players.Count];

            if (currentPlayer.playState != "Playing")
            {
                ChangeTurn(retry + 1);
                return;
            }

            Console.WriteLine("Current Player: " + currentPlayer.Color);
        }
        public bool IsTeammate(string playerColor, string targetColor)
        {
            if ((playerColor == "Red" && targetColor == "Yellow") || (playerColor == "Yellow" && targetColor == "Red"))
                return true;

            if ((playerColor == "Green" && targetColor == "Blue") || (playerColor == "Blue" && targetColor == "Green"))
                return true;

            return false;
        }
        public Piece GetPiece(List<Piece> pieces, string name)
        {
            foreach (var piece in pieces)
                if (piece.Moveable && piece.Name == name)
                {
                    if (piece.Position == -1 && diceValue != 6) // If the piece is at start (-1) and dice roll is not 6, it's not eligible for selection
                    {
                        return null;
                    }
                    return piece;
                }
            return null;
        }
        public List<Player> checkGameOver()
        {
            List<Player> winners = new List<Player>();

            List<Player> playersHome = players.Where(p => p.playState == "Home").ToList();
            List<Player> playersActive = players.Where(p => p.playState == "Playing").ToList();
            List<Player> playersLeft = players.Where(p => p.playState == "Left").ToList();
            
            if (gameMode != "Client")
            {
                if (gameMode == "Computer") {
                    //If the game mode is computer, the game is over when any player reaches home
                    //If any player leaves the game is over and the rest of the players win
                    // If any player reaches home, they win
                    if (playersHome.Count > 0)
                        return playersHome;

                    // If any player leaves, the remaining players win
                    if (playersLeft.Count > 0)
                        return playersActive;
                }
                else
                if (gameType == "2" || gameType == "3" || gameType == "4")
                {
                    //The game is over when any player reaches home he is the winner and the rest are losers
                    //If all players leaves the game and only 1 player is left he is the winner
                    // If any player reaches "Home", they are the winner
                    if (playersHome.Count > 0)
                        return playersHome;

                    // If only one player is left playing, they are the winner
                    if (playersActive.Count == 1 && playersLeft.Count == players.Count - 1)
                        return playersActive;
                }
                else
                if (gameType == "22")
                {
                    //The game is over when any player reaches home and also his partenr is also in home the they both win the other team loses
                    //If the player leaves and his partner also leaves the other team wins
                    //team config is red yellow, green blue

                    // In team mode (Red-Yellow & Green-Blue)
                    var redYellowTeam = players.Where(p => p.Color == "Red" || p.Color == "Yellow").ToList();
                    var greenBlueTeam = players.Where(p => p.Color == "Green" || p.Color == "Blue").ToList();

                    // If both Red & Yellow are Home, they win
                    if (redYellowTeam.All(p => p.playState == "Home"))
                        return redYellowTeam;

                    // If both Green & Blue are Home, they win
                    if (greenBlueTeam.All(p => p.playState == "Home"))
                        return greenBlueTeam;

                    // If one team leaves, the other team wins
                    if (redYellowTeam.All(p => p.playState == "Left"))
                        return greenBlueTeam;

                    if (greenBlueTeam.All(p => p.playState == "Left"))
                        return redYellowTeam;
                }
            }
            return winners;
        }
    }
}