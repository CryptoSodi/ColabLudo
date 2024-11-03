namespace LudoClient.CoreEngine
{
    public class GamePlayer
    {
        private List<GameAction> gameHistory;
        private int currentIndex = 0;

        public GamePlayer(string serializedHistory)
        {
            gameHistory = Newtonsoft.Json.JsonConvert.DeserializeObject<List<GameAction>>(serializedHistory);
        }

        public async Task ReplayGameAsync()
        {
            foreach (var action in gameHistory)
            {
                await PlayActionAsync(action);
                await Task.Delay(500); // Delay to mimic real-time play (or adjust as needed)
            }
        }

        private async Task PlayActionAsync(GameAction action)
        {
            switch (action.ActionType)
            {
                case "RollDice":
                    // Simulate dice roll
                    Console.WriteLine($"{action.PlayerColor} rolled {action.DiceValue}");
                    break;

                case "MovePiece":
                    // Move the piece to the recorded position
                    Console.WriteLine($"{action.PlayerColor} moves {action.PieceName} to {action.NewPosition}");
                    // Call a method to visually move the piece if desired
                    break;

                case "Kill":
                    Console.WriteLine($"{action.PlayerColor} killed opponent's piece at {action.NewPosition}");
                    break;

                case "ChangeTurn":
                    Console.WriteLine("Turn changed.");
                    break;

                default:
                    throw new InvalidOperationException("Unknown action type.");
            }
        }
    }

}
