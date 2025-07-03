using DocumentFormat.OpenXml.Office2010.PowerPoint;
using SharedCode.CoreEngine;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xamarin.Essentials;

namespace AiEngine
{
    class Program
    {
        static String playerColor = "Red";
        static String gameType = "4";
        static String gameMode = "";        
        // This is the AI engine for the game.
        static public Engine engine;
        static Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to the AI Engine for the Ludo Game!");

            args = Environment.GetCommandLineArgs();
            if (args.Length >= 4 && int.TryParse(args[1], out int gameIndexSave) && int.TryParse(args[2], out int windowX) && int.TryParse(args[3], out int windowY))
            {
                Console.WriteLine($"Received values: GameIndexSave={gameIndexSave}, X={windowX}, Y={windowY}");
                GameExperienceExporter.gameIndex = gameIndexSave;
            }
            else
            {
                Console.WriteLine("No int value received.");
            }
            engine = new Engine("AI", gameType, gameType == "22" ? "4" : gameType, playerColor, "");
            engine.StopDice += new Engine.CallbackEventHandler(StopDice);
            engine.AnimateDice += new Engine.Callback_AnimateDice_EventHandler(AnimateDice);
            engine.StartProgressAnimation += new Engine.CallbackEventHandlerStartProgressAnimation(StartProgressAnimation);
            engine.StopProgressAnimation += new Engine.CallbackEventHandlerStopProgressAnimation(StopProgressAnimation);
            engine.RelocateAsync += new Engine.CallbackEventHandlerRelocateAsync(RelocateAsync);
            engine.ShowResults += new Engine.CallbackEventHandlerShowResults(ShowResults);
            engine.PlayerLeftSeat += new Engine.CallbackEventHandlerPlayerLeft(PlayerLeftSeat);
            ExecuteAutoPlayLogic();
            
            Task.Delay(100);
            Console.ReadLine();
        }
        public static void AnimateDice(string SeatName)
        {
            // This method is called to animate the dice.
           // Console.WriteLine($"Animating dice for {SeatName}");
        }
        public static void StopDice(string SeatName, int dicevalue)
        {
            // This method is called to stop the dice.
            Console.WriteLine($"Stopping dice for {SeatName} with value {dicevalue}");
        }
        public static async void StartProgressAnimation(string SeatName)
        {
            while (engine.processing)
            {
                await Task.Delay(10);
            }
            ExecuteAutoPlayLogic();
        }
        public static void StopProgressAnimation(string SeatName)
        {
            // This method is called to stop the progress animation.
            Console.WriteLine($"Stopping progress animation for {SeatName}");
        }
        private static async Task ExecuteAutoPlayLogic()
        {
            if (engine.EngineHelper.checkTurn(engine.EngineHelper.currentPlayer.Color, "RollDice"))
            {
                Console.WriteLine("Client AI Requesting Dice Roll");
                PlayerDiceClicked(engine.EngineHelper.currentPlayer.Color, "", "", "", engine.EngineHelper.gameMode == "Client");
            }
            else
            {
                string result1 = engine.EngineHelper.AIRequestPiece(engine.EngineHelper.currentPlayer.Color);
                string piece1String = result1.Split(",")[0];
                string piece2String = result1.Split(",")[1];

                await MovePiece(piece1String, piece2String, engine.EngineHelper.gameMode == "Client");
            }
        }
        public static async void PlayerDiceClicked(String SeatColor, String DiceValue, String Piece1, String Piece2, bool SendToServer = true)
        {
            if (engine.EngineHelper.checkTurn(SeatColor, "RollDice"))
            {
                String result = await engine.SeatTurn(SeatColor, DiceValue, Piece1, Piece2);
                Console.WriteLine($"1 Local : {result}");
                engine.EngineHelper.index++;
            }
        }
        public static async Task MovePiece(String piece1String, String piece2String, bool SendToServer = true)
        {
            String result = "";
            result = await engine.MovePieceAsync(piece1String, piece2String);
            engine.EngineHelper.index++;
            Console.WriteLine(result);
        }
        public static async Task RelocateAsync(List<Piece> piece, Piece pieceClone, string playsound = "move")
        {
            // Perform the relocation animation.
            await RelocateHelper(piece, pieceClone, playsound);
            // **Post-move Phase:**
        }
        public static async Task RelocateHelper(List<Piece> pieces, Piece pieceClone, string playsound = "move")
        {
            engine.EngineHelper.animationBlock = true;
            pieceClone = pieces[0].Clone();            

            if (pieceClone.Location <= pieces[0].Location)
            {
                if (pieceClone.Location != pieces[0].Location)
                    pieceClone.Jump(engine, 1, true);

                string PBC = pieceClone.getPieceBox();
                

                if (pieceClone.Location != pieces[0].Location)
                    await RelocateHelper(pieces, pieceClone, playsound);
                else
                {
                    engine.EngineHelper.animationBlock = false;
                }
            }
            while (engine.EngineHelper.animationBlock)
                await Task.Delay(20);
        }
        public static async Task ShowResults(string seats, string GameType, string GameCost)
        {
            stopwatch.Stop();
            Console.WriteLine($"Execution time: {stopwatch.ElapsedMilliseconds} ms");
            // Get seat details for both winners and add them to the list
            System.Environment.Exit(0);
        }
        public static void PlayerLeftSeat(string SeatColor, bool SendToServer = true)
        {
            // Handle the event when a player leaves their seat.
            Console.WriteLine($"Player {SeatColor} has left the seat.");
            if (SendToServer)
            {
                // Logic to handle player leaving the game on the server.
                Console.WriteLine($"Sending player left notification for {SeatColor} to server.");
            }
            else
            {
                // Logic to handle player leaving locally.
                Console.WriteLine($"Handling local player left for {SeatColor}.");
            }
        }
    }
}