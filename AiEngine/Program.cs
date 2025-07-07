using SharpNeat.Experiments;
using System.Diagnostics;
namespace AiEngine
{
    class Program
    {
        static Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        static void Main(string[] args)
        {

            //  RunSingleGame();
            var trainer = new LudoTrainer();
            // Start training
            Console.WriteLine("Starting AI training...");
            var experiment = trainer.CreateExperiment("ludo.config.json");
            Neat

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }

        //public static void RunSingleGame()
        //{
        //    LudoEvaluator ludoEvaluator = new LudoEvaluator();
        //    var activationFnLib = DefaultActivationFunctionLibrary.CreateLibraryCppn();

        //    // 1. Create genome factory
        //    var genomeFactory = new NeatGenomeFactory(42, 10, activationFnLib);

        //    // 2. Create genome decoder
        //    var activationScheme = NetworkActivationScheme.CreateAcyclicScheme(); // Feedforward
        //    var genomeDecoder = new NeatGenomeDecoder(activationScheme);

        //    int totalGames = 10000;
        //    double totalFitness = 0;

        //    // Track best genome
        //    NeatGenome bestGenome = null;
        //    double bestFitness = double.MinValue;
        //    string genomeFilePath = "";
        //    for (uint i = 1; i <= totalGames; i++)
        //    {
        //        Console.WriteLine($"\n===== Running game #{i} =====");
        //        genomeFactory = new NeatGenomeFactory(42, 10, activationFnLib);

        //        // Create random genome
        //        NeatGenome genome = genomeFactory.CreateGenome(i);

        //        // Decode genome into a neural net
        //        IBlackBox phenome = genomeDecoder.Decode(genome);

        //        if (phenome == null)
        //        {
        //            Console.WriteLine(">> Failed to decode genome!");
        //            continue; // Skip to next game
        //        }

        //        string[] colors = new string[] { "red", "green", "yellow", "blue" };
        //        // Run simulation
        //        var engine = new Engine("AI", "4", "4", "Red");

        //        var fitness = ludoEvaluator.SimulateGame(engine, phenome, 0,playback: false);

        //        Console.WriteLine($">> Game #{i} fitness: {fitness}");

        //        totalFitness += fitness;

        //        // Check if this genome is better
        //        if (fitness > bestFitness)
        //        {
        //            bestFitness = fitness;
        //            bestGenome = genome;
        //            Console.WriteLine($">> New best genome found! Fitness: {bestFitness}");

        //            // Save best genome to file
        //            genomeFilePath = $"best-genome-{i}.xml";
        //            SaveBestGenome(bestGenome, genomeFilePath);
        //            Console.WriteLine($">> Best genome saved to {genomeFilePath}");
        //        }
        //    }

        //    double avgFitness = totalFitness / totalGames;

        //    Console.WriteLine($"\n===== All games complete =====");
        //    Console.WriteLine($">> Average Fitness over {totalGames} games: {avgFitness}");
        //    Console.WriteLine($">> {genomeFilePath} Best Fitness: {bestFitness}");
        //}
        //public static void SaveBestGenome(NeatGenome bestGenome, string filePath)
        //{
        //    if (bestGenome != null)
        //    {
        //        using (XmlWriter xw = XmlWriter.Create(filePath))
        //        {
        //            NeatGenomeXmlIO.WriteComplete(xw, bestGenome, true);
        //        }
        //        Console.WriteLine($"Best genome saved to {filePath}");
        //    }
        //}
    }

}