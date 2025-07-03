using ClosedXML.Excel;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        }

        private static async Task TrainAi()
        {
            await AiController.MultiActionTrainer.TrainAi();
        }

        public static async Task GenerateAsync() // Fixed: Changed method to 'static' and corrected spelling.
        {
            Console.WriteLine("Enter the next game index to start from:");
            nextGameIndex = int.Parse(Console.ReadLine());
            // Path to the MAUI app EXE
            string secondAppPath = @"C:\Users\tassa\source\repos\LudoClient\AiEngine\bin\Release\net9.0\AiEngine.exe";
            Console.WriteLine("How many games to run at a time? (default 20)");
            int columns = int.Parse(Console.ReadLine());     // Number of games per row            
            int rows = 1;         // Number of rows
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
}
