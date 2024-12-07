using LudoClient.Constants;

namespace LudoClient.CoreEngine
{
    public class GameAction
    {
        public int GameId = GlobalConstants.GameHistorySaveIndex;
        public string redPiece1;
        public string redPiece2;
        public string redPiece3;
        public string redPiece4;
        public string grePiece1;
        public string grePiece2;
        public string grePiece3;
        public string grePiece4;
        public string yelPiece1;
        public string yelPiece2;
        public string yelPiece3;
        public string yelPiece4;
        public string bluPiece1;
        public string bluPiece2;
        public string bluPiece3;
        public string bluPiece4;
        public string PlayerColor { get; set; }
        public string ActionType { get; set; } // e.g., "RollDice", "MovePiece", "Kill", "ChangeTurn"
        public int DiceValue { get; set; }
        public string PieceName { get; set; }
        public string Location { get; set; }
        public string NewPosition { get; set; }
        public string Killed { get; set; }
        public string Safe { get; set; }
        public string GetPiecePosition(string pieceName, Engine engine)
        {
            if (pieceName == null)
                return "";
            Piece piece = engine.EngineHelper.players.SelectMany(p => p.Pieces).FirstOrDefault(p => p.Name == pieceName);
            if (piece == null)
            {
                return pieceName.Substring(0, 1) + 56;
            }
            else
                return engine.EngineHelper.getPieceBox(piece);
            // Return the position if all checks pass
        }
        public GameAction(Engine engine, string playerColor, string actionType, int diceValue, string pieceName, int location, int newPosition, bool killed = false)
        {
            redPiece1 = GetPiecePosition("red1", engine);
            redPiece2 = GetPiecePosition("red2", engine);
            redPiece3 = GetPiecePosition("red3", engine);
            redPiece4 = GetPiecePosition("red4", engine);
            grePiece1 = GetPiecePosition("gre1", engine);
            grePiece2 = GetPiecePosition("gre2", engine);
            grePiece3 = GetPiecePosition("gre3", engine);
            grePiece4 = GetPiecePosition("gre4", engine);
            yelPiece1 = GetPiecePosition("yel1", engine);
            yelPiece2 = GetPiecePosition("yel2", engine);
            yelPiece3 = GetPiecePosition("yel3", engine);
            yelPiece4 = GetPiecePosition("yel4", engine);
            bluPiece1 = GetPiecePosition("blu1", engine);
            bluPiece2 = GetPiecePosition("blu2", engine);
            bluPiece3 = GetPiecePosition("blu3", engine);
            bluPiece4 = GetPiecePosition("blu4", engine);

            PlayerColor = playerColor;
            ActionType = actionType;
            DiceValue = diceValue;
            PieceName = pieceName;
            Location = location != -1 ? location.ToString() : "";
          
            NewPosition = GetPiecePosition(pieceName, engine);
            Killed = killed ? "1" : "0";

            if (pieceName == null)
                Safe = "0";
            Piece piece = engine.EngineHelper.players.SelectMany(p => p.Pieces).FirstOrDefault(p => p.Name == pieceName);
            if (piece == null)
                Safe = "0";
            if (Safe == "0")
                return;

            Safe = engine.EngineHelper.safeZone.Contains(piece.Position) ? "1" : "0";
        }
    }
}
