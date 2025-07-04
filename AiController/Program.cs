using ClosedXML.Excel;
using CsvHelper;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static AiController.LudoActionTrainer;
namespace AiController
{
    internal class Program
    {
        static int nextGameIndex = 0; // Global counter for GameIndexSave
        static void ConvertExcelToCsv(string excelFilePath, string outputCsvPath)
        {
            using (var workbook = new XLWorkbook(excelFilePath))
            {
                foreach (var worksheet in workbook.Worksheets)
                {
                    // If multiple sheets, append sheet name to avoid overwriting
                    string csvFileName = outputCsvPath;
                    if (workbook.Worksheets.Count > 1)
                    {
                        var sheetFileName = Path.Combine(Path.GetDirectoryName(outputCsvPath),Path.GetFileNameWithoutExtension(outputCsvPath) + $"_{worksheet.Name}.csv");
                        csvFileName = sheetFileName;
                    }

                    using (var writer = new StreamWriter(csvFileName))
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        foreach (var row in worksheet.RowsUsed())
                        {
                            foreach (var cell in row.Cells())
                            {
                                csv.WriteField(cell.GetValue<string>());
                            }
                            csv.NextRecord();
                        }
                    }

                    Console.WriteLine($"Saved CSV: {csvFileName}");
                }
            }
        }

        static void Converter(string folder)
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
                .ToList();

            if (!files.Any())
            {
                Console.WriteLine("No XLSX files found in " + folder);
                return;
            }

            Parallel.ForEach(files, xlsx =>
            {
                var fi = new FileInfo(xlsx);
                if (fi.Length < 1024)
                {
                    Console.WriteLine($"Deleting small/invalid XLSX: {fi.Name} ({fi.Length} bytes)");
                    try { File.Delete(xlsx); }
                    catch (Exception ex) { Console.WriteLine($"Failed delete: {ex.Message}"); }
                    return;
                }

                var csv = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(xlsx) + ".csv");
                if (File.Exists(csv))
                {
                    Console.WriteLine($"Skipping already converted: {csv}");
                    return;
                }

                try
                {
                    ConvertExcelToCsv(xlsx, csv);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to convert {xlsx}: {ex.Message}");
                }
            });
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine("Welcome to the Ludo AI Controller!");
            Console.WriteLine("Convert : Train or Generate? input = 1:2:3!");
            nextGameIndex = int.Parse(Console.ReadLine());
            if(nextGameIndex == 1)
            {
                Converter(@"D:\LudoAiData\");
            }
            else if (nextGameIndex == 2)
            {
                await TrainAi();
            }
            else
            {
                await GenerateAsync(); // Fixed: Call the method using 'await' and ensure it is static.
            }
            Console.ReadLine();
        }

        private static async Task TrainAi()
        {
            IDataView data = AiController.LudoActionTrainer.mlContext.Data.LoadFromTextFile<LudoInput>(
                            path: AiController.LudoActionTrainer.dataPath,
                            hasHeader: true,
                            separatorChar: ',');
            AiController.LudoActionTrainer.TrainAndSaveFilteredModel(1, data);
            //AiController.LudoActionTrainer.TrainAndSaveFilteredModel(2, data);
            //AiController.LudoActionTrainer.TrainAndSaveFilteredModel(3, data);
            //AiController.LudoActionTrainer.TrainAndSaveFilteredModel(4, data);
        }
        public static int gamesStarted = 0; // Track total games launched        

        public static async Task GenerateAsync() // Fixed: Changed method to 'static' and corrected spelling.
        {
            Console.WriteLine("Enter the next game index to start from:");
            nextGameIndex = int.Parse(Console.ReadLine());
            // Path to the app EXE
            string secondAppPath = @"C:\Users\tassa\source\repos\LudoClient\AiEngine\bin\Release\net9.0\AiEngine.exe";
            Console.WriteLine("How many games to run at a time? (default 20)");
            int columns = int.Parse(Console.ReadLine());     // Number of games per row            
            Console.WriteLine("How many Total games to run? (default 1000)");
            int totalGames = int.Parse(Console.ReadLine()); // Total number of games




            var tasks = new List<Task>();

            // Launch initial batch of games
            for (int i = 0; i < columns; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    while (true)
                    {
                        int currentGameIndex;

                        lock (typeof(Program)) // Ensure thread-safe increment
                        {
                            if (gamesStarted >= totalGames)
                                break; // Stop launching new games

                            currentGameIndex = nextGameIndex++;
                            gamesStarted++;
                        }

                        Console.WriteLine($"Starting game #{currentGameIndex}");

                        await StartGameAsync(secondAppPath, currentGameIndex);

                        Console.WriteLine($"Game #{currentGameIndex} finished.");
                    }
                }));

                // Optional: Small delay between launching parallel tasks
                await Task.Delay(500);
            }

            Console.WriteLine($"Started running up to {totalGames} games with {columns} in parallel.");
            await Task.WhenAll(tasks); // Wait for all tasks to complete
        }
        public static async Task StartGameAsync(string exePath, int gameIndex)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = gameIndex.ToString(), // Pass the game index if needed
                    UseShellExecute = false
                }
            };

            process.Start();

            // Optionally wait for it to finish
            process.WaitForExit();
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
}
