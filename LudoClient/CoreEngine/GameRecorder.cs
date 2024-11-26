namespace LudoClient.CoreEngine
{
    public static class GameRecorder
    {
        public static Engine engine = null;
        public static List<GameAction> gameHistory = new List<GameAction>();

        public static void EncodeAction(GameAction action)
        {
            gameHistory.Add(action);
        }
        public static string SerializeHistory()
        {
            // Convert the game history to JSON (or other format if preferred)
            return Newtonsoft.Json.JsonConvert.SerializeObject(gameHistory);
        }
        // Record an action for the encoder
        private static void RecordAction(string actionType, int diceValue, Player player, Piece piece = null, int location = -1, int newPosition = -1, bool killed = false)
        {
            var action = new GameAction(player.Color, actionType, diceValue, piece?.Name, location, newPosition, killed);
            gameHistory.Add(action);
        }
        // Method to record a dice roll
        public static void RecordDiceRoll(Player player, int diceValue)
        {
            RecordAction("RollDice", diceValue, player);
        }
        // Method to record a move action
        public static void RecordMove(int diceValue, Player player, Piece piece, int newPosition, bool killed = false)
        {
            RecordAction("MovePiece", diceValue, player, piece, piece.Location, newPosition, killed);
        }
        // Save the game history to a file
        public static void SaveGameHistory()
        {

            // Get the startup directory of the application
            string startupPath = AppDomain.CurrentDomain.BaseDirectory;

            // Define the file path for saving the history file in the startup directory
            string filePath = Path.Combine(startupPath, "GameHistory.json");

            // Serialize the game history to JSON format
            var serializedHistory = Newtonsoft.Json.JsonConvert.SerializeObject(gameHistory);

            // Write the serialized history to the file
            File.WriteAllText(filePath, serializedHistory);

            Console.WriteLine($"Game history saved at: {filePath}");
        }
        public static async Task ReplayGameAsync(string fileName)
        {
            // Get the startup directory of the application
            string startupPath = AppDomain.CurrentDomain.BaseDirectory;

            // Define the file path for saving the history file in the startup directory
            string filePath = Path.Combine(startupPath, fileName);

            if (!File.Exists(filePath))
            {
                Console.WriteLine("Game history file not found.");
                return;
            }

            string historyData = File.ReadAllText(filePath);
            var actions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<GameAction>>(historyData);
            int count = 0;
            foreach (var action in actions)
            {
                count++;
                if (count >= 340) //118
                {
                    await Task.Delay(1000);
                }
                Console.WriteLine($"Replaying action {count}...");
                await Task.Delay(100); // Delay to mimic real-time play (adjust as needed)
                await PlayActionAsync(action);
            }
        }
        static int DiceValue = 0;
        public static int RequestDice()
        {
            return DiceValue;
        }
            private static async Task PlayActionAsync(GameAction action)
        {
            switch (action.ActionType)
            {
                case "RollDice":
                    Console.WriteLine($"{action.PlayerColor} rolled a {action.DiceValue}");
                    DiceValue = action.DiceValue;
                    engine.SeatTurn(action.PlayerColor);
                    break;
                case "MovePiece":
                    Console.WriteLine($" moved  to {action.PieceName}");
                    engine.MovePieceAsync(action.PieceName);
                    break;

                case "Kill":
                    Console.WriteLine($"{action.PlayerColor} killed an opponent piece at {action.NewPosition}");
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