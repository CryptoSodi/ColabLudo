namespace SharedCode.CoreEngine
{
    public class Player
    {
        public int Score = 0;
        public bool CanEnterGoal = false;
        public string Color { get; private set; }
        public List<Piece> Pieces { get; private set; }
        public int StartPosition { get; private set; }
        public string playState = "Playing";
        public Player(string color)
        {
            Color = color;
            Pieces = InitializePieces(color);
            
            StartPosition = new Dictionary<string, int>
            {
                { "red", 0 },
                { "green", 13 },
                { "yellow", 26 },
                { "blue", 39 }
            }[color];
        }
        private List<Piece> InitializePieces(string color)
        {
            return color switch
            {
                "red" => new List<Piece>
                {
                    new Piece(color, "red1"),
                    new Piece(color, "red2"),
                    new Piece(color, "red3"),
                    new Piece(color, "red4")
                },
                "green" => new List<Piece>
                {
                    new Piece(color, "gre1"),
                    new Piece(color, "gre2"),
                    new Piece(color, "gre3"),
                    new Piece(color, "gre4")
                },
                "yellow" => new List<Piece>
                {
                    new Piece(color, "yel1"),
                    new Piece(color, "yel2"),
                    new Piece(color, "yel3"),
                    new Piece(color, "yel4")
                },
                "blue" => new List<Piece>
                {
                    new Piece(color, "blu1"),
                    new Piece(color, "blu2"),
                    new Piece(color, "blu3"),
                    new Piece(color, "blu4")
                },
                _ => new List<Piece>()
            };
        }
    }
}