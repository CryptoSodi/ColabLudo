using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LudoClient.CoreEngine
{
    public class Engine
    {
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
        public Engine()
        {
            players = new List<Player>
            {
                new Player("red"),
                new Player("green"),
                new Player("blue"),
                new Player("yellow")
            };
            currentPlayerIndex = 0;
            board = new Piece[52];
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
        public void CalculateLocation() { 
            
        }
    }
}
