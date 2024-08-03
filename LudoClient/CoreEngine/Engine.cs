using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LudoClient.CoreEngine.Engine;

namespace LudoClient.CoreEngine
{
    public class Engine
    {
        Dictionary<string, int[]> originalPath = new Dictionary<string, int[]>();
        public class Piece
        {
            public string Color { get; private set; }
            public int Position { get; set; }
            public Piece(string color)
            {
                Color = color;
                Position = -1; // -1 indicates the piece is in the base
            }
        }
        public class Player
        {
            public string Color { get; private set; }
            public List<Piece> Pieces { get; private set; }
            public int StartPosition { get; private set; }
            public List<int> HomePositions { get; private set; }
            public Player(string color)
            {
                Color = color;
                Pieces = new List<Piece>
            {
                new Piece(color),
                new Piece(color),
                new Piece(color),
                new Piece(color)
            };
                StartPosition = new Dictionary<string, int>
            {
                { "red", 0 },
                { "green", 13 },
                { "blue", 26 },
                { "yellow", 39 }
            }[color];
                HomePositions = new List<int>();
                for (int i = 0; i < 6; i++)
                {
                    HomePositions.Add((StartPosition + 50 + i) % 52);
                }
            }
        }

        private List<Player> players;
        private int currentPlayerIndex;
        private Piece[] board;
        public void pupulate(Gui gui)
        {
            Grid.SetRow(gui.red1, originalPath["hr0"][0]);
            Grid.SetColumn(gui.red1, originalPath["hr0"][1]);
            Grid.SetRow(gui.red2, originalPath["hr1"][0]);
            Grid.SetColumn(gui.red2, originalPath["hr1"][1]);
            Grid.SetRow(gui.red3, originalPath["hr2"][0]);
            Grid.SetColumn(gui.red3, originalPath["hr2"][1]);
            Grid.SetRow(gui.red4, originalPath["hr3"][0]);
            Grid.SetColumn(gui.red4, originalPath["hr3"][1]);

            Grid.SetRow(gui.gre1, originalPath["hg0"][0]);
            Grid.SetColumn(gui.gre1, originalPath["hg0"][1]);
            Grid.SetRow(gui.gre2, originalPath["hg1"][0]);
            Grid.SetColumn(gui.gre2, originalPath["hg1"][1]);
            Grid.SetRow(gui.gre3, originalPath["hg2"][0]);
            Grid.SetColumn(gui.gre3, originalPath["hg2"][1]);
            Grid.SetRow(gui.gre4, originalPath["hg3"][0]);
            Grid.SetColumn(gui.gre4, originalPath["hg3"][1]);

            Grid.SetRow(gui.blu1, originalPath["hb0"][0]);
            Grid.SetColumn(gui.blu1, originalPath["hb0"][1]);
            Grid.SetRow(gui.blu2, originalPath["hb1"][0]);
            Grid.SetColumn(gui.blu2, originalPath["hb1"][1]);
            Grid.SetRow(gui.blu3, originalPath["hb2"][0]);
            Grid.SetColumn(gui.blu3, originalPath["hb2"][1]);
            Grid.SetRow(gui.blu4, originalPath["hb3"][0]);
            Grid.SetColumn(gui.blu4, originalPath["hb3"][1]);

            Grid.SetRow(gui.yel1, originalPath["hy0"][0]);
            Grid.SetColumn(gui.yel1, originalPath["hy0"][1]);
            Grid.SetRow(gui.yel2, originalPath["hy1"][0]);
            Grid.SetColumn(gui.yel2, originalPath["hy1"][1]);
            Grid.SetRow(gui.yel3, originalPath["hy2"][0]);
            Grid.SetColumn(gui.yel3, originalPath["hy2"][1]);
            Grid.SetRow(gui.yel4, originalPath["hy3"][0]);
            Grid.SetColumn(gui.yel4, originalPath["hy3"][1]);
        }
        public Engine(Gui gui)
        {
            gui.red1.location = -1;
            gui.red2.location = -1;
            gui.red3.location = -1;
            gui.red4.location = -1;

            players = new List<Player>
            {
                new Player("red"),
                new Player("green"),
                new Player("blue"),
                new Player("yellow")
            };
            currentPlayerIndex = 0;

            board = new Piece[52];

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

            originalPath["r0"] = new int[] { 13, 7 };
            originalPath["r1"] = new int[] { 12, 7 };
            originalPath["r2"] = new int[] { 11, 7 };
            originalPath["r3"] = new int[] { 10, 7 };
            originalPath["r4"] = new int[] { 9, 7 };
            originalPath["r5"] = new int[] { 8, 7 };

            originalPath["g0"] = new int[] { 7, 1 };
            originalPath["g1"] = new int[] { 7, 2 };
            originalPath["g2"] = new int[] { 7, 3 };
            originalPath["g3"] = new int[] { 7, 4 };
            originalPath["g4"] = new int[] { 7, 5 };
            originalPath["g5"] = new int[] { 7, 6 };

            originalPath["y0"] = new int[] { 1, 7 };
            originalPath["y1"] = new int[] { 2, 7 };
            originalPath["y2"] = new int[] { 3, 7 };
            originalPath["y3"] = new int[] { 4, 7 };
            originalPath["y4"] = new int[] { 5, 7 };
            originalPath["y5"] = new int[] { 6, 7 };

            originalPath["b0"] = new int[] { 7, 13 };
            originalPath["b1"] = new int[] { 7, 12 };
            originalPath["b2"] = new int[] { 7, 11 };
            originalPath["b3"] = new int[] { 7, 10 };
            originalPath["b4"] = new int[] { 7, 9 };
            originalPath["b5"] = new int[] { 7, 8 };

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
        }
        private int RollDice()
        {
            Random rnd = new Random();
            return rnd.Next(1, 7);
        }
        private void MovePiece(Player player, int pieceIndex, int steps)
        {
            Piece piece = player.Pieces[pieceIndex];
            if (piece.Position == -1)
            {
                if (steps == 6)
                {
                    piece.Position = player.StartPosition;
                    board[player.StartPosition] = piece;
                }
            }
            else
            {
                int newPosition = (piece.Position + steps) % 52;
                if (board[newPosition] != null && board[newPosition].Color != player.Color)
                {
                    SendPieceHome(board[newPosition]);
                }
                board[piece.Position] = null;
                piece.Position = newPosition;
                board[newPosition] = piece;
                if (IsInHomeArea(player, piece))
                {
                    player.Pieces.Remove(piece);
                    Console.WriteLine($"{player.Color} piece has reached home!");
                }
            }
        }
        private void SendPieceHome(Piece piece)
        {
            piece.Position = -1;
        }

        private bool IsInHomeArea(Player player, Piece piece)
        {
            return player.HomePositions.Contains(piece.Position);
        }

        private void PlayTurn()
        {
            Player player = players[currentPlayerIndex];
            int diceRoll = RollDice();
            Console.WriteLine($"{player.Color} rolled a {diceRoll}");

            bool moveMade = false;
            for (int i = 0; i < player.Pieces.Count; i++)
            {
                if (player.Pieces[i].Position != -1 || diceRoll == 6)
                {
                    MovePiece(player, i, diceRoll);
                    moveMade = true;
                    break;
                }
            }

            if (!moveMade)
            {
                Console.WriteLine($"{player.Color} could not move any piece.");
            }

            if (diceRoll != 6)
            {
                currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
            }
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
            while (!IsGameOver())
            {
                PlayTurn();
            }
        }
        public void CalculateLocation()
        {

        }
    }
}
