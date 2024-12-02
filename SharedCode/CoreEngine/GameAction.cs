using SharedCode.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedCode.CoreEngine
{
    public class GameAction
    {
        public static string GetPiecePosition(string pieceName)
        {
            if (pieceName == null)
                return "";
            Piece piece = EngineHelper.players.SelectMany(p => p.Pieces).FirstOrDefault(p => p.Name == pieceName);
            if (piece == null)
            {
                return pieceName.Substring(0, 1) + 56;
            }
            else
                return EngineHelper.getPieceBox(piece);
            // Return the position if all checks pass
        }
        public int GameId = GlobalConstants.GameHistorySaveIndex;
        public string redPiece1 = GetPiecePosition("red1");
        public string redPiece2 = GetPiecePosition("red2");
        public string redPiece3 = GetPiecePosition("red3");
        public string redPiece4 = GetPiecePosition("red4");
        public string grePiece1 = GetPiecePosition("gre1");
        public string grePiece2 = GetPiecePosition("gre2");
        public string grePiece3 = GetPiecePosition("gre3");
        public string grePiece4 = GetPiecePosition("gre4");
        public string yelPiece1 = GetPiecePosition("yel1");
        public string yelPiece2 = GetPiecePosition("yel2");
        public string yelPiece3 = GetPiecePosition("yel3");
        public string yelPiece4 = GetPiecePosition("yel4");
        public string bluPiece1 = GetPiecePosition("blu1");
        public string bluPiece2 = GetPiecePosition("blu2");
        public string bluPiece3 = GetPiecePosition("blu3");
        public string bluPiece4 = GetPiecePosition("blu4");
        public string PlayerColor { get; set; }
        public string ActionType { get; set; } // e.g., "RollDice", "MovePiece", "Kill", "ChangeTurn"
        public int DiceValue { get; set; }
        public string PieceName { get; set; }
        public string Location { get; set; }
        public string NewPosition { get; set; }
        public string Killed { get; set; }
        public string Safe { get; set; }

        public GameAction(string playerColor, string actionType, int diceValue, string pieceName, int location, int newPosition, bool killed = false)
        {
            redPiece1 = GetPiecePosition("red1");
            redPiece2 = GetPiecePosition("red2");
            redPiece3 = GetPiecePosition("red3");
            redPiece4 = GetPiecePosition("red4");
            grePiece1 = GetPiecePosition("gre1");
            grePiece2 = GetPiecePosition("gre2");
            grePiece3 = GetPiecePosition("gre3");
            grePiece4 = GetPiecePosition("gre4");
            yelPiece1 = GetPiecePosition("yel1");
            yelPiece2 = GetPiecePosition("yel2");
            yelPiece3 = GetPiecePosition("yel3");
            yelPiece4 = GetPiecePosition("yel4");
            bluPiece1 = GetPiecePosition("blu1");
            bluPiece2 = GetPiecePosition("blu2");
            bluPiece3 = GetPiecePosition("blu3");
            bluPiece4 = GetPiecePosition("blu4");

            PlayerColor = playerColor;
            ActionType = actionType;
            DiceValue = diceValue;
            PieceName = pieceName;
            Location = location != -1 ? location.ToString() : "";
          
            NewPosition = GetPiecePosition(pieceName);
            Killed = killed ? "1" : "0";

            if (pieceName == null)
                Safe = "0";
            Piece piece = EngineHelper.players.SelectMany(p => p.Pieces).FirstOrDefault(p => p.Name == pieceName);
            if (piece == null)
                Safe = "0";
            if (Safe == "0")
                return;

            Safe = EngineHelper.safeZone.Contains(piece.Position) ? "1" : "0";
        }
    }
}
