using LudoClient.ControlView;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LudoClient.CoreEngine.Engine;

namespace LudoClient.CoreEngine
{
    public class Engine
    {
        Dictionary<string, int[]> originalPath = new Dictionary<string, int[]>();
        public Gui gui;
        public class Piece
        {
            public Token piece;
            public string name = "";
            public bool moveable = false;
            internal int location;

            public string Color { get; private set; }
            public int Position { get; set; }
            public Piece(string color, string name, Token piece)
            {
                this.piece = piece;
                this.name = name;
                Color = color;
                if (name == "red1")
                {
                    Position = 50; Position = -1;
                }
                else
                    Position = -1; // -1 indicates the piece is in the base

            }
        }
        public class Player
        {
            public string Color { get; private set; }
            public List<Piece> Pieces { get; private set; }
            public int StartPosition { get; private set; }
            public Player(string color, Gui gui)
            {
                Color = color;
                if (color == "red")
                {
                    Pieces = new List<Piece>
                    {
                        new Piece(color,gui.red1.name, gui.red1),
                        new Piece(color,gui.red2.name, gui.red2),
                        new Piece(color,gui.red3.name, gui.red3),
                        new Piece(color,gui.red4.name, gui.red4)
                    };
                }

                if (color == "green")
                {
                    Pieces = new List<Piece>
                    {
                        new Piece(color,gui.gre1.name, gui.gre1),
                        new Piece(color,gui.gre2.name, gui.gre2),
                        new Piece(color,gui.gre3.name, gui.gre3),
                        new Piece(color,gui.gre4.name, gui.gre4)
                    };
                }

                if (color == "yellow")
                {
                    Pieces = new List<Piece>
                    {
                        new Piece(color,gui.yel1.name, gui.yel1),
                        new Piece(color,gui.yel2.name, gui.yel2),
                        new Piece(color,gui.yel3.name, gui.yel3),
                        new Piece(color,gui.yel4.name, gui.yel4)
                    };
                }

                if (color == "blue")
                {
                    Pieces = new List<Piece>
                    {
                        new Piece(color,gui.blu1.name, gui.blu1),
                        new Piece(color,gui.blu2.name, gui.blu2),
                        new Piece(color,gui.blu3.name, gui.blu3),
                        new Piece(color,gui.blu4.name, gui.blu4)
                    };
                }
                StartPosition = new Dictionary<string, int>
                {
                    { "red", 0 },
                    { "green", 13 },
                    { "yellow", 26 },
                    { "blue", 39 }
                }[color];
            }
        }

        private List<Player> players;
        private int currentPlayerIndex;
        private Piece[] board;
        public void pupulate(Gui gui)
        {
            //players[0].Pieces[0].location = 50;
            //players[0].Pieces[0].Position = 49;
            for (int i = 0; i < players.Count; i++)
            {
                for (int j = 0; j < players[i].Pieces.Count; j++)
                {
                    Relocate(players[i], players[i].Pieces[j]);
                }
            }
        }
        public Engine(Gui gui)
        {
            this.gui = gui;
            gui.red1.location = gui.red2.location = gui.red3.location = gui.red4.location = gui.gre1.location = gui.gre2.location = gui.gre3.location = gui.gre4.location = gui.blu1.location = gui.blu2.location = gui.blu3.location = gui.blu4.location = gui.yel1.location = gui.yel2.location = gui.yel3.location = gui.yel4.location = -1;

            players = new List<Player>
            {
                new Player("red",gui),
                new Player("green",gui),
                new Player("yellow",gui),
                new Player("blue",gui)
            };
            currentPlayerIndex = 0;

            board = new Piece[57];

            originalPath["p0"] = new int[] { 13, 6 };
            originalPath["p1"] = new int[] { 12, 6 };
            originalPath["p2"] = new int[] { 11, 6 };
            originalPath["p3"] = new int[] { 10, 6 };
            originalPath["p4"] = new int[] { 9, 6 };
            originalPath["p5"] = new int[] { 8, 5 };
            originalPath["p6"] = new int[] { 8, 4 };
            originalPath["p7"] = new int[] { 8, 3 };
            originalPath["p8"] = new int[] { 8, 2 };
            originalPath["p9"] = new int[] { 8, 1 };
            originalPath["p10"] = new int[] { 8, 0 };
            originalPath["p11"] = new int[] { 7, 0 };
            originalPath["p12"] = new int[] { 6, 0 };
            originalPath["p13"] = new int[] { 6, 1 };
            originalPath["p14"] = new int[] { 6, 2 };
            originalPath["p15"] = new int[] { 6, 3 };
            originalPath["p16"] = new int[] { 6, 4 };
            originalPath["p17"] = new int[] { 6, 5 };
            originalPath["p18"] = new int[] { 5, 6 };
            originalPath["p19"] = new int[] { 4, 6 };
            originalPath["p20"] = new int[] { 3, 6 };
            originalPath["p21"] = new int[] { 2, 6 };
            originalPath["p22"] = new int[] { 1, 6 };
            originalPath["p23"] = new int[] { 0, 6 };
            originalPath["p24"] = new int[] { 0, 7 };
            originalPath["p25"] = new int[] { 0, 8 };
            originalPath["p26"] = new int[] { 1, 8 };
            originalPath["p27"] = new int[] { 2, 8 };
            originalPath["p28"] = new int[] { 3, 8 };
            originalPath["p29"] = new int[] { 4, 8 };
            originalPath["p30"] = new int[] { 5, 8 };
            originalPath["p31"] = new int[] { 6, 9 };
            originalPath["p32"] = new int[] { 6, 10 };
            originalPath["p33"] = new int[] { 6, 11 };
            originalPath["p34"] = new int[] { 6, 12 };
            originalPath["p35"] = new int[] { 6, 13 };
            originalPath["p36"] = new int[] { 6, 14 };
            originalPath["p37"] = new int[] { 7, 14 };
            originalPath["p38"] = new int[] { 8, 14 };
            originalPath["p39"] = new int[] { 8, 13 };
            originalPath["p40"] = new int[] { 8, 12 };
            originalPath["p41"] = new int[] { 8, 11 };
            originalPath["p42"] = new int[] { 8, 10 };
            originalPath["p43"] = new int[] { 8, 9 };
            originalPath["p44"] = new int[] { 9, 8 };
            originalPath["p45"] = new int[] { 10, 8 };
            originalPath["p46"] = new int[] { 11, 8 };
            originalPath["p47"] = new int[] { 12, 8 };
            originalPath["p48"] = new int[] { 13, 8 };
            originalPath["p49"] = new int[] { 14, 8 };
            originalPath["p50"] = new int[] { 14, 7 };
            originalPath["p51"] = new int[] { 14, 6 };

            originalPath["r51"] = new int[] { 13, 7 };
            originalPath["r52"] = new int[] { 12, 7 };
            originalPath["r53"] = new int[] { 11, 7 };
            originalPath["r54"] = new int[] { 10, 7 };
            originalPath["r55"] = new int[] { 9, 7 };
            originalPath["r56"] = new int[] { 8, 7 };

            originalPath["g51"] = new int[] { 7, 1 };
            originalPath["g52"] = new int[] { 7, 2 };
            originalPath["g53"] = new int[] { 7, 3 };
            originalPath["g54"] = new int[] { 7, 4 };
            originalPath["g55"] = new int[] { 7, 5 };
            originalPath["g56"] = new int[] { 7, 6 };

            originalPath["y51"] = new int[] { 1, 7 };
            originalPath["y52"] = new int[] { 2, 7 };
            originalPath["y53"] = new int[] { 3, 7 };
            originalPath["y54"] = new int[] { 4, 7 };
            originalPath["y55"] = new int[] { 5, 7 };
            originalPath["y56"] = new int[] { 6, 7 };

            originalPath["b51"] = new int[] { 7, 13 };
            originalPath["b52"] = new int[] { 7, 12 };
            originalPath["b53"] = new int[] { 7, 11 };
            originalPath["b54"] = new int[] { 7, 10 };
            originalPath["b55"] = new int[] { 7, 9 };
            originalPath["b56"] = new int[] { 7, 8 };

            originalPath["hr0"] = new int[] { 11, 2 };
            originalPath["hr1"] = new int[] { 11, 3 };
            originalPath["hr2"] = new int[] { 12, 2 };
            originalPath["hr3"] = new int[] { 12, 3 };

            originalPath["hg0"] = new int[] { 2, 2 };
            originalPath["hg1"] = new int[] { 2, 3 };
            originalPath["hg2"] = new int[] { 3, 2 };
            originalPath["hg3"] = new int[] { 3, 3 };

            originalPath["hy0"] = new int[] { 2, 11 };
            originalPath["hy1"] = new int[] { 2, 12 };
            originalPath["hy2"] = new int[] { 3, 11 };
            originalPath["hy3"] = new int[] { 3, 12 };

            originalPath["hb0"] = new int[] { 11, 11 };
            originalPath["hb1"] = new int[] { 11, 12 };
            originalPath["hb2"] = new int[] { 12, 11 };
            originalPath["hb3"] = new int[] { 12, 12 };

            pupulate(gui);

            rolls.Add(6);
            rolls.Add(1); rolls.Add(6);
            rolls.Add(1); rolls.Add(6);
            rolls.Add(1); rolls.Add(6);
            rolls.Add(1);
            rolls.Add(1);
            rolls.Add(1);
            rolls.Add(1);
            rolls.Add(1);
            rolls.Add(1);
            rolls.Add(1);
            rolls.Add(1);
            rolls.Add(1);
            rolls.Add(1);
            rolls.Add(1);
            rolls.Add(1);
            rolls.Add(1);
            rolls.Add(1);
            rolls.Add(1);
            rolls.Add(1);
            rolls.Add(1);
            rolls.Add(1);
            rolls.Add(1);
            rolls.Add(1);
            rolls.Add(1);
            rolls.Add(1);
            rolls.Add(1);
            rolls.Add(1);
            rolls.Add(1);
        }
        Random rnd = new Random();
        List<int> rolls = new List<int>();
        int index = 0;
        private int RollDice()
        {

            //perform the animation of the dice rolling
            //
            try
            {
                //return rnd.Next(1, 7);
                return rolls[index++];
            }
            catch (Exception)
            {
                return 1;
            }
        }
        private Piece getPiece(List<Piece> pieces, string name)
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                if (pieces[i].moveable && pieces[i].name == name)
                {
                    if (pieces[i].Position == -1 && diceValue != 6)
                    {
                        return null;
                    }
                    return pieces[i];
                }
            }
            return null;
        }
     
        private void performTurnChecks(bool killed)
        {
            gameState = "RollDice";
            if (!killed)
                if (diceValue != 6)
                {

                    ChangeTurn();
                }
                else
                {
                    //If Auto Play Enabled Auto Move the piece
                    //After Movement is completed change the turn
                }
            diceValue = 0;
        }

        List<int> safeZone = [0, 8, 13, 21, 26, 34, 39, 47, 52, 53, 54, 55, 56, 57, -1];
        private bool IsPieceSafe(Player player, Piece piece)
        {
            return safeZone.Contains(piece.Position);
        }
        List<int> home = [52, 11, 24, 37];
        public bool checkTurn(String SeatName,String GameState)
        {
            Player player = players[currentPlayerIndex];
            if (player.Color == SeatName && gameState == GameState)
            { 
                return true;
            }else 
                return false;
        }
        public async void SeatTurn(String SeatName)
        {
            int tempDice = -1;
            Player player = players[currentPlayerIndex];
            if (player.Color == SeatName && gameState == "RollDice")
            {
                diceValue = RollDice();
                tempDice = diceValue;
                int moveablePieces = 0;
                int closedPieces = 0;
                for (int i = 0; i < player.Pieces.Count; i++)
                {
                    if(player.Pieces[i].location == 0 && diceValue == 6)
                    {
                        //open the token
                        player.Pieces[i].moveable = true;
                        moveablePieces++;
                        closedPieces++;
                    }
                    else if ((player.Pieces[i].location + diceValue <= 57) && player.Pieces[i].location != 0)
                    {
                        player.Pieces[i].moveable = true;
                        moveablePieces++;
                    }
                    else player.Pieces[i].moveable = false;
                }

                Console.WriteLine($"{player.Color} rolled a {diceValue} " + $"can move " + moveablePieces + " pieces.");
                encoder($"{player.Color}");
                if (moveablePieces == 1)
                {
                    Console.WriteLine("Turn Animation of the moveable piece;");
                    //If Auto Play Enabled Auto Move the piece
                    //After Movement is completed change the turn
                    gameState = "MovePiece";
                    for (int i = 0; i < player.Pieces.Count; i++)
                    {
                        if (player.Pieces[i].moveable)
                        {
                            MovePiece(player.Pieces[i].name);
                            break;
                        }
                    }
                }
                else
                if (moveablePieces == player.Pieces.Count && diceValue == 6 && closedPieces == player.Pieces.Count)
                {
                    gameState = "MovePiece";
                    MovePiece(player.Pieces[rnd.Next(0, player.Pieces.Count)].name);
                }
                else
                if (moveablePieces > 0)
                {
                    Console.WriteLine("Turn Animation of the moveable pieces;");
                    //Turn Animation of the moveable pieces;
                    //Start the timer for the auto play of 10 seconds. at the end of the timer auto perform the action of move on a random piece or drop the turn.
                    gameState = "MovePiece";
                }
                else
                {
                    Console.WriteLine($"{player.Color} could not move any piece.");
                    ChangeTurn();
                    gameState = "RollDice";
                }
            }
            else
            {
                Console.WriteLine("Not the turn of the player");

            }
            await Sleep(rnd.Next(1, 500));//simulating the server delay
            StopDice(SeatName, tempDice);
        }
        async Task Sleep(int delay)
        {
            await Task.Delay(delay);
        }
        public void MovePiece(String Piece)
        {
            Player player = players[currentPlayerIndex];
            Piece piece = getPiece(player.Pieces, Piece);
            if (piece == null || diceValue == 0)
                return;//Exit not the Current player Piece
            if (gameState == "MovePiece" && piece.moveable)
            {
                bool killed = false;
                if (piece.Position == -1 && diceValue == 6)
                {
                    piece.Position = player.StartPosition;
                    piece.location = 1;
                    board[player.StartPosition] = piece;
                    //perform the animation of the piece moving from base to the start position
                    encoder(piece.name);
                    Relocate(player, piece);
                }
                else
                {
                    if ((piece.location + diceValue) <= 57)
                    {
                        int newPosition = ((piece.Position + diceValue) % 52);

                        if (board[newPosition] != null && board[newPosition].Color != player.Color)
                        {
                            killed = true;
                            board[newPosition].Position = -1;
                            board[newPosition].location = 0;

                            Relocate(player, board[newPosition]);
                        }
                        board[piece.Position] = null;

                        piece.Position = newPosition;
                        piece.location = ((piece.location + diceValue));
                        board[newPosition] = piece;
                        
                        encoder(piece.name);
                        Relocate(player, piece);
                        if (piece.location == 57)
                        {
                            player.Pieces.Remove(piece);
                            Console.WriteLine($"{player.Color} piece has reached home!");
                        }
                        if(player.Pieces.Count==0)
                            Console.WriteLine($"{player.Color} has won the game!");
                    }
                }
                //     checkKills(player,piece);
                performTurnChecks(killed);
                //perform turn turn check
            }
        }
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
        public void Relocate(Player player, Piece piece)
        {
            //piece.Position
            //player.StartPosition
            String pj = "p" + (piece.Position);
            if (piece.Position == -1)
            {
                pj = "h" +piece.name.Substring(0, 1)+(Int32.Parse(piece.name.Substring(3, 1))-1);
            }
            else
            if (piece.location > 51 && piece.location < 58)
            {
                pj = piece.name.Substring(0, 1) + (piece.location -1);
            }

            if (piece.location > 57)
            {

            }
            else
            {
                
              //  piece.piece.TranslateTo(originalPath[pj][1] * 50, originalPath[pj][0] * 50, 1000, Easing.Linear);   


                Grid.SetRow(piece.piece, originalPath[pj][0]);
                Grid.SetColumn(piece.piece, originalPath[pj][1]);
                Console.WriteLine($"{piece.name} is at {pj}");
            }
        }
        public int chainIndex = 0;
        public string chain = "";
        public void encoder(String command) { //encode the game state
            chain += chainIndex+","+command+ "+";
            chainIndex++;
            //Console.WriteLine(chain);
        }
        public void decoder()
        {
            string[] chainArray = chain.Split('+');
            foreach (string item in chainArray)
            {
                string[] parts = item.Split(',');
                int index = int.Parse(parts[0]);
                string command = parts[1];

                // Perform actions based on the decoded command
                switch (command)
                {
                    case "red":
                        // Handle red player command
                        break;
                    case "green":
                        // Handle green player command
                        break;
                    case "yellow":
                        // Handle yellow player command
                        break;
                    case "blue":
                        // Handle blue player command
                        break;
                    default:
                        // Handle unknown command
                        break;
                }
            }
        }
        public void chaser()
        {

        }
        public delegate void CallbackEventHandler(string SeatName,int diceValue);
        public event CallbackEventHandler StopDice;

        int diceValue = 0;
        String gameState = "RollDice";
    }
}
