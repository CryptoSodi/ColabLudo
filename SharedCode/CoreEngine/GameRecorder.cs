using SharedCode.Constants;

namespace SharedCode.CoreEngine
{
    public class GameRecorder
    {
        public Engine engine = null;
        public List<GameAction> gameHistory = new List<GameAction>();
        public int DiceValue = 0;
        public GameRecorder(Engine engine)
        {
            this.engine = engine;
        }
        public void EncodeAction(GameAction action)
        {
            gameHistory.Add(action);
        }
        public string SerializeHistory()
        {
            // Convert the game history to JSON (or other format if preferred)
            return Newtonsoft.Json.JsonConvert.SerializeObject(gameHistory);
        }
        // Record an action for the encoder
        private void RecordAction(string actionType, int diceValue, Player player, Piece piece = null, int location = -1, int newPosition = -1, bool killed = false)
        {
            GameAction action = new GameAction(engine, player.Color, actionType, diceValue, piece?.Name, location, newPosition, killed);
            gameHistory.Add(action);
        }
        // Method to record a dice roll
        public void RecordDiceRoll(Player player, int diceValue)
        {
            RecordAction("RollDice", diceValue, player);
        }
        // Method to record a move action
        public void RecordMove(int diceValue, Player player, Piece piece, int newPosition, bool killed = false)
        {
            RecordAction("MovePiece", diceValue, player, piece, piece.Location, newPosition, killed);
        }
        // Save the game history to a file
        public void SaveGameHistory()
        {
            // Get the startup directory of the application AppDomain.CurrentDomain.BaseDirectory+ 
            // Define the directory and file path
            string startupPath = "C:\\GameData\\";
            GlobalConstants.GameHistorySaveIndex = Directory.GetFiles(startupPath).Length;

            string filePath = Path.Combine(startupPath, GlobalConstants.GameHistorySaveIndex + ".csv");
            
            // Ensure the directory exists
            Directory.CreateDirectory(startupPath);

            // Open a StreamWriter for the CSV file
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                // Write the header  GameId	TurnId	red1	red2	red3	red4	gre1	gre2	gre3	gre4	DiceValue	PieceName	Location	NewPosition	Killed	Safe	Color_green	Color_red	Action_MovePiece	Action_RollDice

                writer.WriteLine(
                    "GameId,TurnId,red1,red2,red3,red4,gre1,gre2,gre3,gre4," +
                    "yel1,yel2,yel3,yel4,blu1,blu2,blu3,blu4," +
                    "isRed,isGreen,isYellow,isBlue," +
                    "isRollDice,isMovePiece," +
                    "isRed1,isRed2,isRed3,isRed4," +
                    "isGreen1,isGreen2,isGreen3,isGreen4," +
                    "isYellow1,isYellow2,isYellow3,isYellow4," +
                    "isBlue1,isBlue2,isBlue3,isBlue4," +
                    "DiceValue,Location,NewPosition,Killed,Safe"
                ); 
                int TurnId = 0;
                // Write each entry as a CSV row
                foreach (var entry in gameHistory)
                {
                    int isRed = entry.PlayerColor == "red" ? 1 : 0;
                    int isGreen = entry.PlayerColor == "green" ? 1 : 0;
                    int isYellow = entry.PlayerColor == "yellow" ? 1 : 0;
                    int isBlue = entry.PlayerColor == "blue" ? 1 : 0;

                    int isRollDice = entry.ActionType == "RollDice" ? 1 : 0;
                    int isMovePiece = entry.ActionType == "MovePiece" ? 1 : 0;

                    int isRed1 = entry.PieceName == "red1" ? 1 : 0;
                    int isRed2 = entry.PieceName == "red2" ? 1 : 0;
                    int isRed3 = entry.PieceName == "red3" ? 1 : 0;
                    int isRed4 = entry.PieceName == "red4" ? 1 : 0;

                    int isGreen1 = entry.PieceName == "gre1" ? 1 : 0;
                    int isGreen2 = entry.PieceName == "gre2" ? 1 : 0;
                    int isGreen3 = entry.PieceName == "gre3" ? 1 : 0;
                    int isGreen4 = entry.PieceName == "gre4" ? 1 : 0;

                    int isYellow1 = entry.PieceName == "yel1" ? 1 : 0;
                    int isYellow2 = entry.PieceName == "yel2" ? 1 : 0;
                    int isYellow3 = entry.PieceName == "yel3" ? 1 : 0;
                    int isYellow4 = entry.PieceName == "yel4" ? 1 : 0;

                    int isBlue1 = entry.PieceName == "blu1" ? 1 : 0;
                    int isBlue2 = entry.PieceName == "blu2" ? 1 : 0;
                    int isBlue3 = entry.PieceName == "blu3" ? 1 : 0;
                    int isBlue4 = entry.PieceName == "blu4" ? 1 : 0;

                    // Extract values from the entry and write them as a single line
                    string csvRow = $"{entry.GameId},{TurnId++},{entry.redPiece1},{entry.redPiece2},{entry.redPiece3},{entry.redPiece4}," +
                                    $"{entry.grePiece1},{entry.grePiece2},{entry.grePiece3},{entry.grePiece4}," +
                                    $"{entry.yelPiece1},{entry.yelPiece2},{entry.yelPiece3},{entry.yelPiece4}," +
                                    $"{entry.bluPiece1},{entry.bluPiece2},{entry.bluPiece3},{entry.bluPiece4}," +
                                    $"{isRed},{isGreen},{isYellow},{isBlue},{isRollDice},{isMovePiece},"+
                                    $"{isRed1},{isRed2},{isRed3},{isRed4}," +
                                    $"{isGreen1},{isGreen2},{isGreen3},{isGreen4}," +
                                    $"{isYellow1},{isYellow2},{isYellow3},{isYellow4}," +
                                    $"{isBlue1},{isBlue2},{isBlue3},{isBlue4}," +
                                    $"{entry.DiceValue},{entry.Location},{entry.NewPosition},{entry.Killed},{entry.Safe}";
                    writer.WriteLine(csvRow);
                }
            }

            Console.WriteLine($"Game history saved as CSV at: {filePath}");
        }        
        public async Task ReplayGameAsync(string fileName)
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
        public int RequestDice()
        {
            return DiceValue;
        }
        private async Task PlayActionAsync(GameAction action)
        {
            switch (action.ActionType)
            {
                case "RollDice":
                    Console.WriteLine($"{action.PlayerColor} rolled a {action.DiceValue}");
                    DiceValue = action.DiceValue;
                    engine.SeatTurn(action.PlayerColor, action.DiceValue+"", "", "");
                    break;
                case "MovePiece":
                    Console.WriteLine($" moved  to {action.PieceName}");
                    engine.MovePieceAsync(action.PieceName, "");
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