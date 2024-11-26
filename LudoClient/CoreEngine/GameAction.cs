using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LudoClient.CoreEngine
{
    public class GameAction
    {
        public string PlayerColor { get; set; }
        public string ActionType { get; set; } // e.g., "RollDice", "MovePiece", "Kill", "ChangeTurn"
        public int DiceValue { get; set; }
        public string PieceName { get; set; }
        public int Location { get; set; }
        public int NewPosition { get; set; }
        public bool Killed { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public GameAction(string playerColor, string actionType, int diceValue, string pieceName,int location, int newPosition, bool killed = false)
        {
            PlayerColor = playerColor;
            ActionType = actionType;
            DiceValue = diceValue;
            PieceName = pieceName;
            Location = location;
            NewPosition = newPosition;
            Killed = killed;
        }
    }
}
