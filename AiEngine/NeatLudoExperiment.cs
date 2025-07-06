using DocumentFormat.OpenXml.Drawing;
using SharedCode.CoreEngine;
using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.Decoders.Neat;
using SharpNeat.DistanceMetrics;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.Neat;
using SharpNeat.Network;
using SharpNeat.Phenomes;
using SharpNeat.SpeciationStrategies;
using System.Xml;

namespace AiEngine
{
    public class LudoEvaluator : IPhenomeEvaluator<IBlackBox>
    {
        private ulong _evaluationCount;
        private bool _stopConditionSatisfied;

        public ulong EvaluationCount => _evaluationCount;
        public bool StopConditionSatisfied => _stopConditionSatisfied;
        public FitnessInfo Evaluate(IBlackBox phenome)
        {
            _evaluationCount++;

            var engine = new Engine("AI", "4", "4", "Red");
            double fitness = SimulateGame(engine, phenome);
         //   Console.WriteLine(fitness);
            //double scaledFitness = fitness * 1000; // scale up
            //scaledFitness = Math.Max(0.001, scaledFitness); // ensure > 0
            //Console.WriteLine($"Fitness (scaled): {scaledFitness}");
            return new FitnessInfo(fitness, fitness);
        }
        public void Reset()
        {
            // Reset any state between evaluations if needed.
        }
        public double SimulateGame(Engine engine, IBlackBox phenome, bool playback = false)
        {
            double fitness = 0;
            const int maxTurns = 150;
            int aiTurnCount = 0;
        retry:
            while (engine.PlayState != "Stop" && aiTurnCount < maxTurns)
            {
                string seatColor = engine.EngineHelper.currentPlayer.Color;
                if (engine.EngineHelper.checkTurn(seatColor, "RollDice"))
                {
                    if (playback)
                        Console.WriteLine("AI is rolling the dice...");

                    string result = engine.SeatTurn(seatColor, "", "", "").GetAwaiter().GetResult();

                    if (playback)
                        Console.WriteLine($"Dice Roll Result: {result}");

                    engine.EngineHelper.index++;
                }
                else
                {
                    if (seatColor == "red")
                    {
                        double[] state = LudoStateExtractor.ExtractState(engine);
                        for (int i = 0; i < state.Length; i++)
                            phenome.InputSignalArray[i] = state[i];

                        phenome.Activate();

                        var (piece1, piece2) = LudoActionMapper.MapOutput(phenome.OutputSignalArray, engine);
                        string moveResult = engine.MovePieceAsync(piece1, piece2).GetAwaiter().GetResult();
                       
                        fitness += engine.fitness(0, true);

                       
                            //Console.WriteLine($"AI Move: {piece1} {piece2}, Result: {moveResult}");
                            if (moveResult == ",")
                        {
                            aiTurnCount++; // Only increment when AI makes a move
                            goto retry; // Try again if move failed.
                        }

                        engine.EngineHelper.index++;
                    }
                    else
                    {
                        string opponentMove = engine.EngineHelper.AIRequestPiece(seatColor);
                        var moveParts = opponentMove.Split(",");
                        string result = engine.MovePieceAsync(moveParts[0], moveParts[1]).GetAwaiter().GetResult();

                        if (playback)
                            Console.WriteLine($"Opponent Move: {moveParts[0]} {moveParts[1]}, Result: {result}");

                        engine.EngineHelper.index++;
                    }
                }
            }
            //Console.WriteLine(fitness);
            if (fitness < 0)
                fitness = 0.01;// If fitness is negative, return 0 to avoid confusion.
            return fitness;
        }
    }

    public static class LudoActionMapper
    {
        // Red pieces, single moves
        private static readonly string[] Pieces = { "red1", "red2", "red3", "red4" };

        // All unique pairs (for double moves)
        private static readonly (int, int)[] Pairs =
        {
        (0, 1), (0, 2), (0, 3),
        (1, 2), (1, 3),
        (2, 3)
    };

        /// <summary>
        /// Maps neural network outputs to a move (piece1, piece2). 
        /// If the highest neuron is 0-3, single move; 4-9, double move.
        /// </summary>
        public static (string, string) MapOutput(ISignalArray outputs, Engine engine)
        {
            // Find neuron with highest value
            int selected = 0;
            double maxVal = double.MinValue;
            for (int i = 0; i < outputs.Length; i++)
            {
                if (outputs[i] > maxVal)
                {
                    maxVal = outputs[i];
                    selected = i;
                }
            }

            // Single piece move (outputs 0–3)
            if (selected < Pieces.Length)
                return (Pieces[selected], "");

            // Double piece move (outputs 4–9)
            int doubleIndex = selected - Pieces.Length;
            if (doubleIndex < Pairs.Length)
            {
                var (firstIdx, secondIdx) = Pairs[doubleIndex];
                return (Pieces[firstIdx], Pieces[secondIdx]);
            }

            // Fallback: No valid move
            return ("", "");
        }
    }

    public static class LudoStateExtractor
    {
        public static double[] ExtractState(Engine engine)
        {
            var inputs = new List<double>();

            // Normalized turn index (log scale)
            inputs.Add(Math.Log10(engine.EngineHelper.index + 1) / Math.Log10(200.0));

            // Normalize dice value (still important)
            inputs.Add(engine.EngineHelper.diceValue / 6.0);

            // Current player's pieces (always include 4 pieces!)
            string[] colors = new string[] { "red", "green", "yellow", "blue" };
            for (int j = 0; j < 4; j++)
            {
                for (int i = 1; i <= 4; i++)
                {
                    var piece = engine.EngineHelper.getPlayer(colors[j]).Pieces.FirstOrDefault(p => p.Name == $"{colors[j].Remove(3)}{i}");

                    if (piece != null)
                    {
                        // Piece is still on board
                        inputs.Add(piece.Location / 57.0);          // Location normalized
                        inputs.Add(piece.Position / 57.0);          // Position normalized
                        if(colors[j] == "red")
                        {
                            inputs.Add(piece.Moveable ? 1.0 : 0.0);     // Can move?
                            inputs.Add(piece.DoubleMoveable ? 1.0 : 0.0); // Can double move?
                        }
                    }
                    else
                    {
                        // Piece is already home
                        inputs.Add(1.0); // Location at end
                        inputs.Add(1.0); // Position at end
                        if (colors[j] == "red")
                        {
                            inputs.Add(0.0);     // Can move?
                            inputs.Add(0.0); // Can double move?
                        }
                    }
                }
            }

            return inputs.ToArray();
        }
    }

    public class LudoTrainer
    {
        private NeatEvolutionAlgorithm<NeatGenome> _ea;
        private NeatGenome _bestGenome;

        public void StartTraining()
        {
            var activationFnLib = DefaultActivationFunctionLibrary.CreateLibraryCppn();

            var genomeFactory = new NeatGenomeFactory(42, 10, activationFnLib);
            var genomeList = genomeFactory.CreateGenomeList(100, 0);
            if (genomeList.Count == 0)
                throw new Exception("No genomes were generated for evaluation.");
            var neatParams = new NeatEvolutionAlgorithmParameters
            {
                SpecieCount = 10,
                ElitismProportion = 0.1
            };

            var distanceMetric = new ManhattanDistanceMetric();
            var speciationStrategy = new KMeansClusteringStrategy<NeatGenome>(distanceMetric);
            var complexityRegulation = new NullComplexityRegulationStrategy();

            _ea = new NeatEvolutionAlgorithm<NeatGenome>(
                neatParams,
                speciationStrategy,
                complexityRegulation
            );

            var genomeDecoder = new NeatGenomeDecoder(NetworkActivationScheme.CreateAcyclicScheme());
            var evaluator = new LudoEvaluator();
            var genomeListEvaluator = new ParallelGenomeListEvaluator<NeatGenome, IBlackBox>(genomeDecoder, evaluator);

            _ea.Initialize(genomeListEvaluator, genomeFactory, genomeList);

            _ea.UpdateEvent += OnUpdateEvent;
            _ea.PausedEvent += OnPausedEvent;
            _ea.StartContinue();
        }

        private void OnUpdateEvent(object sender, EventArgs e)
        {
            Console.WriteLine($"Generation: {_ea.CurrentGeneration}, BestFitness: {_ea.Statistics._maxFitness:F3}");
            _bestGenome = _ea.CurrentChampGenome;

            // Save best genome every 10 generations
            if (_ea.CurrentGeneration % 10 == 0)
            {
                SaveBestGenome("best-genome.xml");
            }
        }

        private void OnPausedEvent(object sender, EventArgs e)
        {
            Console.WriteLine("Training paused. Saving final genome.");
            SaveBestGenome("best-genome-final.xml");
        }
        public void SaveBestGenome(string filePath)
        {
            if (_bestGenome != null)
            {
                using (XmlWriter xw = XmlWriter.Create(filePath))
                {
                    NeatGenomeXmlIO.WriteComplete(xw, _bestGenome, true);
                }
                Console.WriteLine($"Best genome saved to {filePath}");
            }
        }
        public void LoadAndRunBestGenome(string filePath)
        {
            // Load genome
            NeatGenome genome;
            using (XmlReader xr = XmlReader.Create(filePath))
            {
                genome = NeatGenomeXmlIO.ReadGenome(xr, false);
            }

            // Decode genome
            var decoder = new NeatGenomeDecoder(NetworkActivationScheme.CreateAcyclicScheme());
            IBlackBox phenome = decoder.Decode(genome);

            // Evaluate the genome
            var evaluator = new LudoEvaluator();
            double fitness = evaluator.Evaluate(phenome)._fitness;
            Console.WriteLine($"Loaded AI Fitness: {fitness}");

            // Run a playback game
            Console.WriteLine("Starting playback of AI...");
            var engine = new Engine("AI", "4", "4", "Red");
            evaluator.SimulateGame(engine, phenome, playback: true);
        }

        public void StopTraining()
        {
            _ea?.Stop();
        }
    }
}