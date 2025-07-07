using SharedCode.CoreEngine;
using SharpNeat;
using SharpNeat.Evaluation;
using SharpNeat.Experiments;
using SharpNeat.Experiments.ConfigModels;
using SharpNeat.IO;
using SharpNeat.Neat.EvolutionAlgorithm;
using SharpNeat.NeuralNets;

namespace AiEngine
{
    public class LudoEvaluator : IPhenomeEvaluator<IBlackBox<double>>
    {
        public bool StopConditionSatisfied => false;
        public ulong EvaluationCount = 0;
        public void Reset() { }

        public double SimulateGame(Engine engine, IBlackBox<double> phenome, bool playback = false)
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
                    string result = engine.SeatTurn(seatColor, "", "", "").GetAwaiter().GetResult();
                    engine.EngineHelper.index++;
                }
                else
                {
                    if (seatColor == "red")
                    {
                        double[] state = LudoStateExtractor.ExtractState(engine);
                        for (int i = 0; i < state.Length; i++)
                            phenome.Inputs.Span[i] = state[i];

                        phenome.Activate();

                        var (piece1, piece2) = LudoActionMapper.MapOutput(phenome.Outputs.Span, engine);
                        string moveResult = engine.MovePieceAsync(piece1, piece2).GetAwaiter().GetResult();

                        fitness += engine.fitness(0, true);
                        if (playback)
                            Console.WriteLine($"{engine.EngineHelper.index} : {seatColor} Moved: {piece1} X {piece2}, Result: {moveResult} XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX");


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
                            Console.WriteLine($"{engine.EngineHelper.index} : {seatColor} Moved: {moveParts[0]} {moveParts[1]}, Result: {result}");

                        engine.EngineHelper.index++;
                    }
                }
            }
            //Console.WriteLine(fitness);
            if (fitness < 0)
                fitness = 0.01;// If fitness is negative, return 0 to avoid confusion.
            return fitness;
        }

        public FitnessInfo Evaluate(IBlackBox<double> phenome)
        {
            EvaluationCount++;
            var engine = new Engine("AI", "4", "4", "Red");
            // Use the neural network (phenome) to play a game of Ludo and compute fitness
            double fitness = SimulateGame(engine, phenome);
            // Guarantee non-negative, real-valued fitness
            if (double.IsNaN(fitness) || fitness < 0)
                fitness = 0.01;
            return new FitnessInfo(fitness);
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
        public static (string, string) MapOutput(Span<double> outputs, Engine engine)
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
        public static double GetNormalizedBoardPosition(Piece piece)
        {
            // Home: piece is not on board yet (Position == -1)
            if (piece.Position == -1)
                return 0.0; // or any reserved value you want

            // On Board: Position 0–51
            if (piece.Location > 0 && piece.Location <= 51)
                return (piece.Position + 1) / 58.0; // 1..52

            // Home Stretch: Location 52-57
            if (piece.Location > 51 && piece.Location < 58)
                return piece.Location / 57.0; // 52/57 .. 57/57

            // Shouldn't ever get here
            return 0.0;
        }
        public static double[] ExtractState(Engine engine)
        {
            var inputs = new List<double>();

            // Normalized turn index (log scale)
            inputs.Add(Math.Log10(engine.EngineHelper.index + 1) / Math.Log10(1000.0));

            // Normalize dice value (still important)
            inputs.Add(engine.EngineHelper.diceValue / 6.0);

            // Current player's pieces (always include 4 pieces!)
            string[] colors = new string[] { "red", "green", "yellow", "blue" };
            for (int j = 0; j < 4; j++)
            {
                for (int i = 1; i <= 4; i++)
                {
                    Piece piece = engine.EngineHelper.getPlayer(colors[j]).Pieces.FirstOrDefault(p => p.Name == $"{colors[j].Remove(3)}{i}");

                    if (piece != null)
                    {
                        // Piece is still on board
                        inputs.Add(piece.Location / 57.0);              // Location normalized
                        inputs.Add(GetNormalizedBoardPosition(piece));  // Position normalized (0-1 for board, 0-1 for home stretch)
                        // Is Safe can not be killed
                        inputs.Add(engine.EngineHelper.safeZone.Contains(piece.Position)? 1 : 0);
                        // Position normalized (0-51 for board, 52-57 for home)
                        if (colors[j] == "red")
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
                        inputs.Add(1.0); // Is Safe Cannot be killed
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

    public class LudoTrainer : INeatExperimentFactory
    {
        public string Id => "Ludo";

        public INeatExperiment<double> CreateExperiment(Stream jsonConfigStream)
        { 
            // Load experiment JSON config.
            ExperimentConfig experimentConfig = JsonUtils.Deserialize<ExperimentConfig>(jsonConfigStream);
            // Create an evaluation scheme object for the XOR task.
            var evalScheme = new LudoEvaluationScheme<double>();

            // Create a NeatExperiment object with the evaluation scheme,
            // and assign some default settings (these can be overridden by config).
            var experiment = new NeatExperiment<double>(evalScheme, Id)
            {
                IsAcyclic = true,
                ActivationFnName = ActivationFunctionId.LeakyReLU.ToString()
            }; 
            
            // Apply configuration to the experiment instance.
            experiment.Configure(experimentConfig);
            return experiment;
        }

        public INeatExperiment<float> CreateExperimentSinglePrecision(Stream jsonConfigStream)
        {
            throw new NotImplementedException();
        }

        private class LudoEvaluationScheme<T> : IBlackBoxEvaluationScheme<double>
        {
            public int InputCount => 58;

            public int OutputCount => 10;

            public bool IsDeterministic => true;
            /// <inheritdoc/>
            public IComparer<FitnessInfo> FitnessComparer => PrimaryFitnessInfoComparer.Singleton;

            /// <inheritdoc/>
            public FitnessInfo NullFitness => FitnessInfo.DefaultFitnessInfo;

            public bool EvaluatorsHaveState => false;

            public IPhenomeEvaluator<IBlackBox<double>> CreateEvaluator()
            {
                return new LudoEvaluator();
            }

            public bool TestForStopCondition(FitnessInfo fitnessInfo)
            {
                return (fitnessInfo.PrimaryFitness >= 20);
            }
        }
    }
}