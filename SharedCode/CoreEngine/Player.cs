using SharedCode.ControlView;

namespace SharedCode.CoreEngine
{
    public class Player
    {
        public string Color { get; private set; }
        public List<Piece> Pieces { get; private set; }
        public int StartPosition { get; private set; }
        public Player(string color, Gui gui)
        {
            Color = color;
            Pieces = InitializePieces(color, gui);
            foreach (var piece in Pieces)
            {
                EngineHelper.Alayout.Add(piece.PieceToken);
            }

            StartPosition = new Dictionary<string, int>
            {
                { "red", 0 },
                { "green", 13 },
                { "yellow", 26 },
                { "blue", 39 }
            }[color];
        }
        private List<Piece> InitializePieces(string color, Gui gui)
        {
            return color switch
            {
                "red" => new List<Piece>
                {
                    new Piece(color, gui.red1.name, gui.red1),
                    new Piece(color, gui.red2.name, gui.red2),
                    new Piece(color, gui.red3.name, gui.red3),
                    new Piece(color, gui.red4.name, gui.red4)
                },
                "green" => new List<Piece>
                {
                    new Piece(color, gui.gre1.name, gui.gre1),
                    new Piece(color, gui.gre2.name, gui.gre2),
                    new Piece(color, gui.gre3.name, gui.gre3),
                    new Piece(color, gui.gre4.name, gui.gre4)
                },
                "yellow" => new List<Piece>
                {
                    new Piece(color, gui.yel1.name, gui.yel1),
                    new Piece(color, gui.yel2.name, gui.yel2),
                    new Piece(color, gui.yel3.name, gui.yel3),
                    new Piece(color, gui.yel4.name, gui.yel4)
                },
                "blue" => new List<Piece>
                {
                    new Piece(color, gui.blu1.name, gui.blu1),
                    new Piece(color, gui.blu2.name, gui.blu2),
                    new Piece(color, gui.blu3.name, gui.blu3),
                    new Piece(color, gui.blu4.name, gui.blu4)
                },
                _ => new List<Piece>()
            };
        }
    }
}
