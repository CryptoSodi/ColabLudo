using LudoClient.ControlView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LudoClient.CoreEngine
{
    public class Piece
    {
        public Token PieceToken { get; private set; }
        public string Name { get; private set; }
        public bool Moveable { get; set; }
        public int Location { get; set; }
        public string Color { get; private set; }
        public int Position { get; set; }
        public Piece(string color, string name, Token pieceToken)
        {
            PieceToken = pieceToken;
            Name = name;
            Color = color;
            Position = -1; // -1 indicates the piece is in the base
            Moveable = false;
            Location = 0;
        }
    }
}
