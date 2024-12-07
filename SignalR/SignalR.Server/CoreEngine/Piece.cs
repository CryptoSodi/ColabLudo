using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LudoClient.CoreEngine
{
    public class Piece
    {
        public string Name { get; private set; }
        public bool Moveable { get; set; }
        public int Location { get; set; }
        public string Color { get; private set; }
        public int Position { get; set; }
        public Piece(string color, string name)
        {
            Name = name;
            Color = color;
            Position = -1; // -1 indicates the piece is in the base
            Moveable = false;
            Location = 0;
        }
        public Piece Clone()
        {
            return new Piece(this.Color, this.Name) // Assuming Token is reference-safe
            {
                Moveable = this.Moveable,
                Location = this.Location,
                Position = this.Position
            };
        }
        public void Jump(int DiceValue, bool clone=false)
        {
            if (this.Position == -1 && DiceValue == 6)
            {
                if (!clone)
                    Engine.board[EngineHelper.getPieceBox(this)].Remove(this);
                this.Position = EngineHelper.players.Where(p => p.Color == Color).ToList()[0].StartPosition;
                this.Location = 1;
                if (!clone)
                    Engine.board[EngineHelper.getPieceBox(this)].Add(this);
            }
            else if (this.Location + DiceValue <= 57)
            {
                if (!clone)
                    Engine.board[EngineHelper.getPieceBox(this)].Remove(this);
                this.Position = (this.Position + DiceValue) % 52;
                this.Location += DiceValue;
                if (!clone)
                    Engine.board[EngineHelper.getPieceBox(this)].Add(this);
            }
        }
    }
}