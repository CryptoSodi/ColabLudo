namespace SharedCode.CoreEngine
{
    public class Piece
    {
        public string Name { get; private set; }
        public string Color { get; private set; }
        public int Location { get; set; }
        public int Position { get; set; }
        public bool Moveable { get; set; }
        public bool DoubleMoveable { get; internal set; }
        public bool SingleKilable { get; internal set; }
        public bool DoubleKilable { get; internal set; }
        public int Score { get; set; }

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
                Location = this.Location,
                Position = this.Position,
                Moveable = this.Moveable,
                DoubleMoveable = this.DoubleMoveable
            };
        }
        public void Jump(Engine engine, int DiceValue, bool clone=false)
        {
            if (this.Position == -1 && DiceValue == 6)
            {
                if (!clone)
                    engine.board[getPieceBox()].Remove(this);
                this.Position = engine.EngineHelper.players.Where(p => p.Color == Color).ToList()[0].StartPosition;
                this.Location = 1;
                if (!clone)
                    engine.board[getPieceBox()].Add(this);
            }
            else if (this.Location + DiceValue <= 57)
            {
                if (!clone)
                    engine.board[getPieceBox()].Remove(this);
                this.Position = (this.Position + DiceValue) % 52;
                this.Location += DiceValue;
                if (!clone)
                    engine.board[getPieceBox()].Add(this);
            }
        }
        public string getPieceBox()
        {
            //piece.Position
            //player.StartPosition
            string pj = this.Position == -1
                    ? "h" + this.Name.Substring(0, 1) + (int.Parse(this.Name.Substring(3, 1)) - 1)
                    : "p" + this.Position;

            if (this.Location > 51 && this.Location < 58)
                pj = this.Name.Substring(0, 1) + (this.Location - 1);
            return pj;
        }
    }
}