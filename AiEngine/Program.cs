using SharedCode.CoreEngine;
using System.Diagnostics;

namespace AiEngine
{
    class Program
    {
        static Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        static void Main(string[] args)
        {
            var trainer = new LudoTrainer();

            // Start training
            Console.WriteLine("Starting AI training...");
            trainer.StartTraining();

            Console.WriteLine("Press Enter to stop training...");
            Console.ReadLine();
            trainer.StopTraining();

            // Save best genome automatically
            trainer.SaveBestGenome("bestLudoGenome.xml");

            // Load the genome and run evaluation
            Console.WriteLine("Loading and evaluating best AI...");
            trainer.LoadAndRunBestGenome("bestLudoGenome.xml");

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }
    }
}