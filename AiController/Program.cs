using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.FastTree;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
namespace AiController
{
    internal class Program
    {
        static int nextGameIndex = 0; // Global counter for GameIndexSave
        public static void CombineAndSave(string folder, string outputCsv)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            var tempDir = Path.Combine(folder, "__temp_csv");
            Directory.CreateDirectory(tempDir);

            var files = Directory.GetFiles(folder, "game_*.xlsx")
                .OrderBy(f =>
                {
                    var parts = Path.GetFileNameWithoutExtension(f).Split('_');
                    return int.TryParse(parts.Last(), out var idx) ? idx : int.MaxValue;
                })
                .ToArray();

            if (!files.Any())
            {
                Console.WriteLine("No files found in " + folder);
                return;
            }

            foreach (var xlsx in files)
            {
                var csv = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(xlsx) + ".csv");
                Console.WriteLine("Loading" + csv);
                var stream = File.Open(xlsx, FileMode.Open, FileAccess.Read);
                 var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream);
                 var writer = new StreamWriter(csv);

                do
                {
                    while (reader.Read())
                    {
                        var row = Enumerable.Range(0, reader.FieldCount)
                            .Select(i => reader.GetValue(i)?.ToString().Replace("\"", "\"\"") ?? "")
                            .Select(v => v.Contains(',') ? $"\"{v}\"" : v);
                        writer.WriteLine(string.Join(",", row));
                    }
                } while (reader.NextResult());
            }

            Console.WriteLine($"Combined data saved to {outputCsv}");
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine("Welcome to the Ludo AI Controller!");
            Console.WriteLine("Train or Generate? input = 1:2!");
            nextGameIndex = int.Parse(Console.ReadLine());
            if (nextGameIndex == 1)
            {
                CombineAndSave(@"D:\LudoAiData\", @"D:\LudoAiData\combined_training.csv");
                await TrainAi();
            }
            else
            {
                await GenerateAsync(); // Fixed: Call the method using 'await' and ensure it is static.
            }
        }

        private static async Task TrainAi()
        {
            // 2. Create MLContext
            var mlContext = new MLContext(seed: 0);
            // 3. Load data from CSV
            var combinedData = mlContext.Data.LoadFromTextFile<LudoExperienceInput>( path: Path.Combine(@"D:\LudoAiData\__temp_csv", "*.csv"), hasHeader: true, separatorChar: ',');
            // 4. Split dataset
            var split = mlContext.Data.TrainTestSplit(combinedData, testFraction: 0.2);

            var pipeline = mlContext.Transforms.Concatenate(
                    "Features",
                    nameof(LudoExperienceInput.GameIndex),
                    nameof(LudoExperienceInput.TurnIndex),

                    nameof(LudoExperienceInput.RedLocations),
                    nameof(LudoExperienceInput.RedPositions),
                    nameof(LudoExperienceInput.RedMoveable),
                    nameof(LudoExperienceInput.RedDouble),
                    nameof(LudoExperienceInput.RedInSafe),

                    nameof(LudoExperienceInput.GreLocations),
                    nameof(LudoExperienceInput.GrePositions),
                    nameof(LudoExperienceInput.GreMoveable),
                    nameof(LudoExperienceInput.GreDouble),
                    nameof(LudoExperienceInput.GreInSafe),

                    nameof(LudoExperienceInput.YelLocations),
                    nameof(LudoExperienceInput.YelPositions),
                    nameof(LudoExperienceInput.YelMoveable),
                    nameof(LudoExperienceInput.YelDouble),
                    nameof(LudoExperienceInput.YelInSafe),

                    nameof(LudoExperienceInput.BluLocations),
                    nameof(LudoExperienceInput.BluPositions),
                    nameof(LudoExperienceInput.BluMoveable),
                    nameof(LudoExperienceInput.BluDouble),
                    nameof(LudoExperienceInput.BluInSafe),

                    nameof(LudoExperienceInput.PlayerScores),

                    nameof(LudoExperienceInput.CurrentPlayer),
                    nameof(LudoExperienceInput.Action),
                    nameof(LudoExperienceInput.DiceValue)
                )
                .Append(mlContext.Transforms.NormalizeMinMax("Features"))
                .AppendCacheCheckpoint(mlContext)
                .Append(mlContext.Regression.Trainers.FastTree(
                    labelColumnName: nameof(LudoExperienceInput.Reward),
                    featureColumnName: "Features"));

            Console.WriteLine("▶️ Training...");
            var model = pipeline.Fit(split.TrainSet);

            Console.WriteLine("▶️ Evaluating...");
            var predictions = model.Transform(split.TestSet);
            var metrics = mlContext.Regression.Evaluate(predictions, labelColumnName: nameof(LudoExperienceInput.Reward));
            Console.WriteLine($"RMSE: {metrics.RootMeanSquaredError:F3}, R²: {metrics.RSquared:F2}");

            mlContext.Model.Save(model, split.TrainSet.Schema, "ludo_ai_model.zip");
            Console.WriteLine("✅ Model saved as ludo_ai_model.zip");
        }

        public static async Task GenerateAsync() // Fixed: Changed method to 'static' and corrected spelling.
        {
            Console.WriteLine("Enter the next game index to start from:");
            nextGameIndex = int.Parse(Console.ReadLine());
            // Path to the MAUI app EXE
            string secondAppPath = @"C:\Users\tassa\source\repos\LudoClient\SharedCode\bin\Release\net9.0-windows10.0.19041.0\win10-x64\SharedCode.exe";

            int columns = 10;     // Number of games per row
            int rows = 2;         // Number of rows
            int baseWindowX = -5; // Starting X
            int baseWindowY = 0;  // Starting Y
            int offsetX = 400;    // Horizontal gap between windows
            int offsetY = 800;    // Vertical gap between rows

            Console.WriteLine($"Starting {rows * columns} instances of the MAUI app...");

            var tasks = new List<Task>();

            // Launch initial grid of games
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    int windowX = baseWindowX + (col * offsetX);
                    int windowY = baseWindowY + (row * offsetY);

                    // Start each game in its own task and respawn on exit
                    tasks.Add(Task.Run(() =>
                        KeepStartingGames(secondAppPath, windowX, windowY)
                    ));

                    // Add delay between initial launches
                    await Task.Delay(1000);
                }
            }

            Console.WriteLine("All initial games started. Auto-respawn is running.");
            await Task.WhenAll(tasks); // Wait forever
        }
        static void KeepStartingGames(string appPath, int x, int y)
        {
            while (true)
            {
                int gameIndexSave = GetNextGameIndex();

                string arguments = $"{gameIndexSave} {x} {y}";
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = appPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                };

                try
                {
                    using (Process process = Process.Start(processStartInfo))
                    {
                        Console.WriteLine($"Started game {gameIndexSave} at X={x}, Y={y}");
                        process.WaitForExit();
                        Console.WriteLine($"Game {gameIndexSave} exited with code {process.ExitCode}");
                    }
                }
                catch (Exception ex)
                {
                    Task.Delay(500).Wait();
                    using (Process process = Process.Start(processStartInfo))
                    {
                        Console.WriteLine($"Started game {gameIndexSave} at X={x}, Y={y}");
                        process.WaitForExit();
                        Console.WriteLine($"Game {gameIndexSave} exited with code {process.ExitCode}");
                    }
                    Console.WriteLine($"Failed to start game {gameIndexSave}: {ex.Message}");
                }

                // Optional small delay between restarts
                Task.Delay(500).Wait();
            }
        }

        static int GetNextGameIndex()
        {
            // Thread-safe increment of game index
            return System.Threading.Interlocked.Increment(ref nextGameIndex);
        }
    }

    public class LudoExperienceInput
    {
        [LoadColumn(0)] public float GameIndex { get; set; }
        [LoadColumn(1)] public float TurnIndex { get; set; }

        // StateBefore – Red
        [LoadColumn(2, 5), VectorType(4)] public float[] RedLocations { get; set; }
        [LoadColumn(6, 9), VectorType(4)] public float[] RedPositions { get; set; }
        [LoadColumn(10, 13), VectorType(4)] public float[] RedMoveable { get; set; }
        [LoadColumn(14, 17), VectorType(4)] public float[] RedDouble { get; set; }
        [LoadColumn(18, 21), VectorType(4)] public float[] RedInSafe { get; set; }

        // StateBefore – Green
        [LoadColumn(22, 25), VectorType(4)] public float[] GreLocations { get; set; }
        [LoadColumn(26, 29), VectorType(4)] public float[] GrePositions { get; set; }
        [LoadColumn(30, 33), VectorType(4)] public float[] GreMoveable { get; set; }
        [LoadColumn(34, 37), VectorType(4)] public float[] GreDouble { get; set; }
        [LoadColumn(38, 41), VectorType(4)] public float[] GreInSafe { get; set; }

        // StateBefore – Yellow
        [LoadColumn(42, 45), VectorType(4)] public float[] YelLocations { get; set; }
        [LoadColumn(46, 49), VectorType(4)] public float[] YelPositions { get; set; }
        [LoadColumn(50, 53), VectorType(4)] public float[] YelMoveable { get; set; }
        [LoadColumn(54, 57), VectorType(4)] public float[] YelDouble { get; set; }
        [LoadColumn(58, 61), VectorType(4)] public float[] YelInSafe { get; set; }

        // StateBefore – Blue
        [LoadColumn(62, 65), VectorType(4)] public float[] BluLocations { get; set; }
        [LoadColumn(66, 69), VectorType(4)] public float[] BluPositions { get; set; }
        [LoadColumn(70, 73), VectorType(4)] public float[] BluMoveable { get; set; }
        [LoadColumn(74, 77), VectorType(4)] public float[] BluDouble { get; set; }
        [LoadColumn(78, 81), VectorType(4)] public float[] BluInSafe { get; set; }

        [LoadColumn(82, 85), VectorType(4)] public float[] PlayerScores { get; set; }

        // CurrentPlayer + Action
        [LoadColumn(86, 89), VectorType(4)] public float[] CurrentPlayer { get; set; }
        [LoadColumn(90, 93), VectorType(4)] public float[] Action { get; set; }

        // DiceValue
        [LoadColumn(94, 99), VectorType(6)] public float[] DiceValue { get; set; }

        // StateAfter – flattened same as StateBefore (columns 100 to ~181)
        [LoadColumn(100, 103), VectorType(4)] public float[] After_RedLocations { get; set; }
        [LoadColumn(104, 107), VectorType(4)] public float[] After_RedPositions { get; set; }
        [LoadColumn(108, 111), VectorType(4)] public float[] After_RedMoveable { get; set; }
        [LoadColumn(112, 115), VectorType(4)] public float[] After_RedDouble { get; set; }
        [LoadColumn(116, 119), VectorType(4)] public float[] After_RedInSafe { get; set; }

        [LoadColumn(120, 123), VectorType(4)] public float[] After_GreLocations { get; set; }
        [LoadColumn(124, 127), VectorType(4)] public float[] After_GrePositions { get; set; }
        [LoadColumn(128, 131), VectorType(4)] public float[] After_GreMoveable { get; set; }
        [LoadColumn(132, 135), VectorType(4)] public float[] After_GreDouble { get; set; }
        [LoadColumn(136, 139), VectorType(4)] public float[] After_GreInSafe { get; set; }

        [LoadColumn(140, 143), VectorType(4)] public float[] After_YelLocations { get; set; }
        [LoadColumn(144, 147), VectorType(4)] public float[] After_YelPositions { get; set; }
        [LoadColumn(148, 151), VectorType(4)] public float[] After_YelMoveable { get; set; }
        [LoadColumn(152, 155), VectorType(4)] public float[] After_YelDouble { get; set; }
        [LoadColumn(156, 159), VectorType(4)] public float[] After_YelInSafe { get; set; }

        [LoadColumn(160, 163), VectorType(4)] public float[] After_BluLocations { get; set; }
        [LoadColumn(164, 167), VectorType(4)] public float[] After_BluPositions { get; set; }
        [LoadColumn(168, 171), VectorType(4)] public float[] After_BluMoveable { get; set; }
        [LoadColumn(172, 175), VectorType(4)] public float[] After_BluDouble { get; set; }
        [LoadColumn(176, 179), VectorType(4)] public float[] After_BluInSafe { get; set; }

        [LoadColumn(180, 183), VectorType(4)] public float[] After_PlayerScores { get; set; }

        // Other fields
        [LoadColumn(184)] public float Reward { get; set; }
        [LoadColumn(185)] public float SixBonusTurn { get; set; }
        [LoadColumn(186)] public float HadExtraTurn { get; set; }
        [LoadColumn(187)] public float ExtraTurn { get; set; }
        [LoadColumn(188)] public bool Done { get; set; }
    }
}
