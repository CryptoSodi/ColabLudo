using SharedCode.Constants;
using System.Diagnostics;
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

        public delegate Task CallbackEventHandlerRelocateAsync(List<Piece> piece, Piece pieceClone, string playsound);
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
        public Dictionary<string, List<Piece>>? board;

        public EngineHelper EngineHelper = new EngineHelper();
        
        public bool processing = false;

        public async Task<string> TimerTimeoutAsync(String SeatName)
        {
           if (StartProgressAnimation != null)
                StartProgressAnimation(SeatName);
           string result = "";
           switch (EngineHelper.gameState){
                case "RollDice":
                //    result = await SeatTurn(SeatName, "", "");
                    return result;
                case "MovePiece":
                    Player player = EngineHelper.currentPlayer;
                    List<Piece> moveablePieces = player.Pieces.Where(p => p.Moveable).ToList();
                    
               //     await MovePieceAsync(moveablePieces[GlobalConstants.rnd.Next(0,moveablePieces.Count)].Name);
                    result = moveablePieces[GlobalConstants.rnd.Next(0, moveablePieces.Count)].Name;
                    return result;
            }
            return "";
        }
        public Engine(string gameMode, string gameType, string playerCount, string playerColor, string rollsString="")
        {
            processing = false;
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


            EngineHelper.rolls.Add(6);
            EngineHelper.rolls.Add(6);
            EngineHelper.rolls.Add(6);
            EngineHelper.rolls.Add(3);
            EngineHelper.rolls.Add(2);
            EngineHelper.rolls.Add(2);
            EngineHelper.rolls.Add(2);
            EngineHelper.rolls.Add(6);
            EngineHelper.rolls.Add(6);
            EngineHelper.rolls.Add(6);
            EngineHelper.rolls.Add(3);
            EngineHelper.rolls.Add(6);
            EngineHelper.rolls.Add(2);
            EngineHelper.rolls.Add(2);
            EngineHelper.rolls.Add(2);
            EngineHelper.rolls.Add(1);

            if (gameMode == "Server")
                for (int i = 0; i < 5000; i++)
                    EngineHelper.rolls.Add(GlobalConstants.rnd.Next(1, 7));
            if (gameMode == "Client")
                EngineHelper.rolls = rollsString.Select(c => int.Parse(c.ToString())).ToList();

            EngineHelper.rollsString = string.Join("", EngineHelper.rolls);
        }
        public async Task<string> SeatTurn(string seatName, String DiceValue, String Piece1, String Piece2)
        {
            if (PlayState == "Stop")
                return "";

            int tempDice = -1;
            string result = ",";
            string tempPiece1 = "";
            string tempPiece2 = "";
            // Check if it's the correct player's turn and if the game is in the roll state
            if (EngineHelper.checkTurn(seatName, "RollDice"))
            {
                processing = true;
                EngineHelper.gameState = "RollingDice";
                if (AnimateDice!=null)
                    AnimateDice(seatName);

                EngineHelper.diceValue = EngineHelper.RollDice();
                tempDice = EngineHelper.diceValue;

                if (EngineHelper.gameMode != "Server")
                {
                    if (!EngineHelper.stopAnimate)
                        await Task.Delay(200);
                    else
                        await Task.Delay(30);
                }

                if (StopDice!=null)
                    StopDice(seatName, EngineHelper.diceValue);

                // Determine which pieces can move
                foreach (var piece in EngineHelper.currentPlayer.Pieces)
                {
                    piece.Moveable = false;
                    piece.DoubleMoveable = false;

                    if (piece.Location == 0 && EngineHelper.diceValue == 6)// Open the token if it's in base and dice shows a 6
                        piece.Moveable = true;
                    else if (piece.Location != 0)
                    {
                        //The piece is moveable now decide if it can only move alone or double movement is also allowed
                        if ((piece.Location + EngineHelper.diceValue <= 51 && !EngineHelper.currentPlayer.CanEnterGoal) || (piece.Location + EngineHelper.diceValue <= 57 && EngineHelper.currentPlayer.CanEnterGoal))
                        {
                            //Check if the piece is not in the home zone and can move to the home zone zlone
                            bool pathBlocked = false;
                            
                            var Stepperpiece = piece.Clone();
                            for (int step = 1; step < EngineHelper.diceValue; step++)
                            {
                                Stepperpiece.Jump(this, 1, true);

                                string newBox = EngineHelper.getPieceBox(Stepperpiece);
                                List<Piece> tokensAtIntermediate = board?[newBox].Where(p => p.Color != piece.Color && !(EngineHelper.gameType == "22" && EngineHelper.IsTeammate(piece.Color, p.Color))).ToList();

                                if (tokensAtIntermediate?.Count > 1 && !EngineHelper.safeZone.Contains(Stepperpiece.Position))
                                {
                                    pathBlocked = true;
                                    break;
                                }
                            }
                            piece.Moveable = !pathBlocked;
                        }

                        if(EngineHelper.diceValue == 2 || EngineHelper.diceValue == 4 || EngineHelper.diceValue == 6)
                        {
                            // New logic to handle double token jump over a block
                            if (piece.Location <= 51)
                            {
                                // Check if another token is on the same position
                                var samePositionTokens = EngineHelper.currentPlayer.Pieces
                                    .Where(p => p.Position == piece.Position)
                                    .ToList();

                                if (samePositionTokens.Count > 1 && (piece.Location + (EngineHelper.diceValue / 2) <= 51))
                                {   //Double Move is not allowed in the Home Zone
                                    //Allow both tokens to move together
                                    piece.DoubleMoveable = true;
                                }
                            }
                        }
                    }
                }

                List<Piece> moveablePieces = EngineHelper.currentPlayer.Pieces.Where(p => p.Moveable).ToList();
                List<Piece> DoubleMoveablePieces = EngineHelper.currentPlayer.Pieces.Where(p => p.DoubleMoveable).ToList();
                Console.WriteLine($"{EngineHelper.index} : {EngineHelper.currentPlayer.Color} rolled a {EngineHelper.diceValue}. Can move {moveablePieces.Count} D: {DoubleMoveablePieces.Count} pieces. ");

                gameRecorder.RecordDiceRoll(EngineHelper.currentPlayer, EngineHelper.diceValue);

                // Handle possible scenarios based on the number of moveable pieces
                bool moveSeat = false;
                bool moveDouble = false;

                if (moveablePieces.Count > 0 || DoubleMoveablePieces.Count > 0)
                    EngineHelper.gameState = "MovePiece";
                else
                {
                    EngineHelper.animationBlock = false;
                    if (StopProgressAnimation != null)
                        StopProgressAnimation(EngineHelper.currentPlayer.Color);
                    EngineHelper.ChangeTurn(); // Change turn to the next player
                    if (!EngineHelper.stopAnimate && StartProgressAnimation != null)
                        StartProgressAnimation(EngineHelper.currentPlayer.Color);
                    EngineHelper.gameState = "RollDice";
                }

                if (moveablePieces.Count == 1 && DoubleMoveablePieces.Count==0)
                    moveSeat = true;
                else if (moveablePieces.Count > 1 && DoubleMoveablePieces.Count == 0)
                {
                    //If all the moveable pieces are on the same location
                    //Check if we need to move 1 piece or 2 pieces
                    int firstLocation = moveablePieces[0].Location;
                    var piece = moveablePieces[0];
                    int targetPosition = (piece.Position + (EngineHelper.diceValue)) % 52;
                    if (moveablePieces.All(p => p.Location == firstLocation))
                    {
                        moveSeat = true;
                        //// Check if pieces are not at the Home Zone  //If the piece is in the home and can enter home allow to move a single piece if all the pieces are at the same location
                        //if (firstLocation == 0 || firstLocation > 51 && EngineHelper.currentPlayer.CanEnterGoal)
                        //    //Can only move 1 piece if the dice value are uneven
                        //    moveSeat = true;
                        //else if (targetPosition <= 51 && !EngineHelper.currentPlayer.CanEnterGoal || targetPosition <= 57 && EngineHelper.currentPlayer.CanEnterGoal)
                        //{
                        //    moveSeat = true;
                        //}


                    }
                    else
                    {
                        if (!EngineHelper.stopAnimate)
                            // Start timer for auto play or prompt for user action
                            StartProgressAnimation(EngineHelper.currentPlayer.Color);
                        else
                        {
                            Console.WriteLine("PREVENT ANIMATION TIER");
                        }
                    }
                }
                else if (moveablePieces.Count == 0 && DoubleMoveablePieces.Count > 1)
                {
                    int firstLocation = DoubleMoveablePieces[0].Location;
                    if (DoubleMoveablePieces.All(p => p.Location == firstLocation))
                    {
                        moveDouble = true;
                    }
                    else
                    {
                        if (!EngineHelper.stopAnimate)
                            // Start timer for auto play or prompt for user action
                            StartProgressAnimation(EngineHelper.currentPlayer.Color);
                        else
                        {
                            Console.WriteLine("PREVENT ANIMATION TIER");
                        }
                    }
                }
                else if (moveablePieces.Count > 1 && DoubleMoveablePieces.Count > 1)
                {
                    moveSeat = true;
                    moveDouble = true;
                    if (!EngineHelper.stopAnimate)
                        // Start timer for auto play or prompt for user action
                        StartProgressAnimation(EngineHelper.currentPlayer.Color);
                    else
                    {
                        Console.WriteLine("PREVENT ANIMATION TIER");
                    }
                }

                if (moveablePieces.Count > 0 && DoubleMoveablePieces.Count>1)
                {
                    EngineHelper.gameState = "MovePiece";
                    EngineHelper.animationBlock = false;
                    if (StopProgressAnimation != null)
                        StopProgressAnimation(EngineHelper.currentPlayer.Color);
                    if (!EngineHelper.stopAnimate && StartProgressAnimation != null)
                        StartProgressAnimation(EngineHelper.currentPlayer.Color);
                }

                if (moveSeat && !moveDouble)
                {
                    if (!EngineHelper.replay)
                    {
                        if (Piece1 != "")
                            tempPiece1 = Piece1;
                        else
                        {
                            tempPiece1 = moveablePieces.First(p => p.Moveable).Name;
                        }
                        result = await MovePieceAsync(tempPiece1, "");//Move the only moveable piece
                    }
                }
                else if (!moveSeat && moveDouble)
                {
                    if (!EngineHelper.replay)
                    {
                        if (Piece1 != "" && Piece2 != "")
                        {
                            tempPiece1 = Piece1;
                            tempPiece2 = Piece2;
                        }
                        else
                        {
                            tempPiece1 = DoubleMoveablePieces[0].Name;
                            tempPiece2 = DoubleMoveablePieces[1].Name;
                        }
                        result = await MovePieceAsync(tempPiece1, tempPiece2);//Move the only moveable piece
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
            processing = false;
            return $"{tempDice},{result}";
        }
        public async Task<string> MovePieceAsync(String piece1String, String piece2String)
        {
            if (PlayState == "Stop")
                return ",";
            Player player = EngineHelper.currentPlayer;
            Piece piece1 = null;
            Piece piece2 = null;
            Piece piece1Clone = null;
            Piece piece2Clone = null;
            int tempDice = EngineHelper.diceValue;

            if (piece2String!="")
            {
                piece1 = EngineHelper.GetPiece(player.Pieces, piece1String);
                piece2 = EngineHelper.GetPiece(player.Pieces, piece2String);
                piece1Clone = piece1.Clone();
                piece2Clone = piece2.Clone();
            }
            else
            {
                piece1 = EngineHelper.GetPiece(player.Pieces, piece1String);
                piece1Clone = piece1.Clone();
            }

            if (piece1 == null || EngineHelper.diceValue == 0)
                return ","; // Exit if not the current player's piece or no dice roll

            if ((piece1.Moveable || piece1.DoubleMoveable) && EngineHelper.checkTurn(piece1.Name, "MovePiece"))
            {
                processing = true;
                EngineHelper.gameState = "MovingPiece";

                Console.WriteLine($"{EngineHelper.index} : {EngineHelper.currentPlayer.Color} moved a {piece1String} & {piece2String} with dicevalue{EngineHelper.diceValue}.");

                bool killed = false;

                List<Piece> relocatedPieces = new List<Piece>();//Pieces sent to the relocation service to relocate and paint them on the game UI

                EngineHelper.currentPlayer.Score += EngineHelper.diceValue;//INCREASE THE SCORE OF THE PLAYER

                if (piece1.Position == -1 && EngineHelper.diceValue == 6) // Moving from base to start
                {
                    piece1.Jump(this, EngineHelper.diceValue);
                    
                    relocatedPieces.Add(piece1);
                    if (RelocateAsync != null)
                       await RelocateAsync(relocatedPieces, piece1.Clone(), "move");

                    gameRecorder.RecordMove(EngineHelper.diceValue, player, piece1, piece1.Position, killed);
                }
                else if (piece1.Location + EngineHelper.diceValue <= 57) // Normal move within bounds
                {
                    int oldPosition = piece1.Position;
                    string oldBox = EngineHelper.getPieceBox(piece1);
                    if (piece2 != null)
                    {
                        piece2.Jump(this, EngineHelper.diceValue/2);
                        piece1.Jump(this, EngineHelper.diceValue/2);
                    }
                    else
                        piece1.Jump(this, EngineHelper.diceValue);
                    
                    string newBox = EngineHelper.getPieceBox(piece1);
                    int ownAtDest = board?[newBox].Count(x => x.Color == piece1.Color) ?? 0;

                    // List<Piece> kilablePieces = board[pj].Where(p => p.Color != piece.Color).ToList();
                    List<Piece> kilablePieces = board?[newBox].Where(p => p.Color != piece1.Color && !(EngineHelper.gameType == "22" && EngineHelper.IsTeammate(piece1.Color, p.Color))).ToList();
                    
                    var tokensInOldBox = board?[oldBox];

                    if (tokensInOldBox != null && tokensInOldBox.Count != 0 && !EngineHelper.safeZone.Contains(oldPosition))
                    {
                        int ownCount = tokensInOldBox.Count(p => p.Color == piece1.Color);
                        int enemyCount = tokensInOldBox.Count(p => p.Color != piece1.Color && !(EngineHelper.gameType == "22" && EngineHelper.IsTeammate(piece1.Color, p.Color)));

                        if (ownCount == 1 && enemyCount == 1)
                        {
                            // Kill the remaining own piece in the old box
                            var ownTrapped = tokensInOldBox.First(p => p.Color == piece1.Color && p != piece1);
                            var ownTrappedClone = ownTrapped.Clone();
                            ownTrapped.Position = -1;
                            ownTrapped.Location = 0;
                            board?[oldBox].Remove(ownTrapped);
                            board?[EngineHelper.getPieceBox(ownTrapped)].Add(ownTrapped);

                            if (RelocateAsync != null) {
                                relocatedPieces.Add(ownTrapped);
                                EngineHelper.currentPlayer.Score -= 5; // Lose points in case of getting own piece beat by moveing a piece
                                RelocateAsync(relocatedPieces, piece1Clone, "kill");
                            }
                        }
                    }
                    relocatedPieces = new List<Piece>();
                    //Add logic if the killer is 2 pieces and target has 2 killables then kill both
                    // If 2 enemies and after move 2 own tokens => kill both enemies
                    if (kilablePieces?.Count == 2 && ownAtDest == 2 && !EngineHelper.safeZone.Contains(piece1.Position))
                    {
                        if (RelocateAsync != null)
                        {
                            relocatedPieces.Add(piece1);
                            if (piece2!=null)
                                relocatedPieces.Add(piece2);
                            
                            await RelocateAsync(relocatedPieces, piece1Clone, "move");
                        }
                        
                        foreach (var enemy in kilablePieces)
                        {
                            enemy.Position = -1;
                            enemy.Location = 0;
                            board?[newBox].Remove(enemy);
                            board?[EngineHelper.getPieceBox(enemy)].Add(enemy);
                            relocatedPieces = new List<Piece>();
                            relocatedPieces.Add(enemy);
                            
                            if (RelocateAsync != null)
                            {
                                EngineHelper.currentPlayer.Score += 5; // INCREASE SCORE BY KILLED PIECE
                                RelocateAsync(relocatedPieces, relocatedPieces[0], "kill");
                            }
                        }
                        killed = true;
                        EngineHelper.currentPlayer.CanEnterGoal = true;
                    }
                    else if ((kilablePieces?.Count == 1 || kilablePieces?.Count == 3) && !EngineHelper.safeZone.Contains(piece1.Position))
                    {// Prevent killing if there are two or more opponent pieces
                        
                        EngineHelper.currentPlayer.CanEnterGoal = true;//Pieces can move into home now as player killed an opponent
                        Piece killedPiece = kilablePieces[0];
                        killedPiece.Position = -1; // Send opponent's piece back to base
                        killedPiece.Location = 0;
                        board?[newBox].Remove(killedPiece);
                        board?[EngineHelper.getPieceBox(killedPiece)].Add(killedPiece);
                        killed = true;

                        relocatedPieces.Add(piece1);
                        if (piece2 != null)
                            relocatedPieces.Add(piece2);
                        if (RelocateAsync != null)
                            await RelocateAsync(relocatedPieces, piece1Clone, "move");
                        relocatedPieces = new List<Piece>();
                        relocatedPieces.Add(kilablePieces[0]);
                        if (RelocateAsync != null)
                        {
                            EngineHelper.currentPlayer.Score += 5;
                            await RelocateAsync(relocatedPieces, kilablePieces[0], "kill");
                        }
                            
                    }
                    if (!killed && RelocateAsync != null)
                    {
                        relocatedPieces = new();
                        relocatedPieces.Add(piece1);
                        if (piece2 != null)
                            relocatedPieces.Add(piece2);
                        await RelocateAsync(relocatedPieces, piece1Clone, "move");
                    }
                    
                    gameRecorder.RecordMove(EngineHelper.diceValue, player, piece1, piece1.Position, killed); // Prepare animation
                    
                    if (piece1.Location == 57)
                    {
                        EngineHelper.currentPlayer.Score += 10; // Piece reached home score bonus
                        killed = true;
                        player.Pieces.Remove(piece1);

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
                    EngineHelper.currentPlayer.Score += 20; // PLAYER WON THE GAME BONUS
                    Console.WriteLine($"{player.Color} has won the game!");
                    // EngineHelper.players.Remove(player);
                    List<Player> winners = EngineHelper.checkGameOver();
                    if (winners.Count > 0)
                    {
                        //GANE OVER
                        GameOver(winners);
                        processing = false;
                        return piece1String+","+piece2String;
                    }
                }

                if (!EngineHelper.stopAnimate)
                    //perform turn turn check
                    StartProgressAnimation(EngineHelper.currentPlayer.Color);
                else
                    TimerTimeoutAsync(EngineHelper.currentPlayer.Color);
            }
            else
            {
                processing = false;
                return ",";
            }

            processing = false;

            return piece1String + "," + piece2String;
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
        public int index = -1;
        internal int indexServer = -1;

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
            if (SeatNameOrPiece.Contains(","))
                SeatNameOrPiece = SeatNameOrPiece.Split(",")[0];
            return ((currentPlayer.Color == SeatNameOrPiece || currentPlayer.Color.ToLower().Contains(SeatNameOrPiece.Replace("1", "").Replace("2", "").Replace("3", "").Replace("4", ""))) && gameState == GameState);
        }
        public int RollDice()
        {
            if(replay)
            {
                return Engine.gameRecorder.RequestDice();
            }
            if (rolls.Count != 0)
            {
                diceValue = rolls[0];
                rolls.RemoveAt(0);
                return diceValue;
            }
            else
            {
                return GlobalConstants.rnd.Next(1, 7);
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
                if ((piece.Moveable || piece.DoubleMoveable) && piece.Name == name)
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