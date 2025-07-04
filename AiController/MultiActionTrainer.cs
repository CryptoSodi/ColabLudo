using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.LightGbm;
using System;
using System.IO;
using System.Linq;

namespace AiController
{
    public class LudoActionTrainer
    {
        public static readonly string modelOutputDir = "Models";
        public static readonly string dataPath = @"D:\\LudoAiData\\*.csv";

        public class LudoInput
        {
            [LoadColumn(1)] public float TurnIndex { get; set; }
            [LoadColumn(4, 7), VectorType(4)] public float[] RedLocations { get; set; }
            [LoadColumn(8, 11), VectorType(4)] public float[] RedPositions { get; set; }
            [LoadColumn(12, 15), VectorType(4)] public float[] RedMoveable { get; set; }
            [LoadColumn(16, 19), VectorType(4)] public float[] RedDouble { get; set; }
            [LoadColumn(20, 23), VectorType(4)] public float[] RedInSafe { get; set; }

            [LoadColumn(24, 27), VectorType(4)] public float[] GreLocations { get; set; }
            [LoadColumn(28, 31), VectorType(4)] public float[] GrePositions { get; set; }
            [LoadColumn(32, 35), VectorType(4)] public float[] GreMoveable { get; set; }
            [LoadColumn(36, 39), VectorType(4)] public float[] GreDouble { get; set; }
            [LoadColumn(40, 43), VectorType(4)] public float[] GreInSafe { get; set; }

            [LoadColumn(44, 47), VectorType(4)] public float[] YelLocations { get; set; }
            [LoadColumn(48, 51), VectorType(4)] public float[] YelPositions { get; set; }
            [LoadColumn(52, 55), VectorType(4)] public float[] YelMoveable { get; set; }
            [LoadColumn(56, 59), VectorType(4)] public float[] YelDouble { get; set; }
            [LoadColumn(60, 63), VectorType(4)] public float[] YelInSafe { get; set; }

            [LoadColumn(64, 67), VectorType(4)] public float[] BluLocations { get; set; }
            [LoadColumn(68, 71), VectorType(4)] public float[] BluPositions { get; set; }
            [LoadColumn(72, 75), VectorType(4)] public float[] BluMoveable { get; set; }
            [LoadColumn(76, 79), VectorType(4)] public float[] BluDouble { get; set; }
            [LoadColumn(80, 83), VectorType(4)] public float[] BluInSafe { get; set; }

            [LoadColumn(84, 87), VectorType(4)] public float[] PlayerScores { get; set; }
            [LoadColumn(88)] public float CurrentPlayer_1 { get; set; }
            [LoadColumn(89)] public float CurrentPlayer_2 { get; set; }
            [LoadColumn(90)] public float CurrentPlayer_3 { get; set; }
            [LoadColumn(91)] public float CurrentPlayer_4 { get; set; }
            [LoadColumn(92, 97), VectorType(6)] public float[] DiceValue { get; set; }
            [LoadColumn(98)] public float Reward;
            [LoadColumn(99)] public float SixBonusTurn;
            [LoadColumn(100)] public float HadExtraTurn;
            [LoadColumn(101)] public float ExtraTurn;
            [LoadColumn(102)] public float Done;

            [LoadColumn(3)] public float Action { get; set; } // Label
        }
        public static MLContext mlContext = new MLContext(seed: 0);
        public static void TrainAndSaveFilteredModel(int playerIndex, IDataView data)
        {
            Console.WriteLine($"📂 Loading data for {playerIndex}...");
            // We want to select rows where value == 1 → use: lowerBound = 0.9999, upperBound = 1.0001
            var filtered = mlContext.Data.FilterRowsByColumn(data, $"CurrentPlayer_{playerIndex}", lowerBound: 0.9999f, upperBound: 1.0001f);
            var split = mlContext.Data.TrainTestSplit(filtered, testFraction: 0.2);

            var pipeline = mlContext.Transforms.Concatenate("Features", nameof(LudoInput.TurnIndex),
                nameof(LudoInput.RedLocations), nameof(LudoInput.RedPositions), nameof(LudoInput.RedMoveable), nameof(LudoInput.RedDouble), nameof(LudoInput.RedInSafe),
                nameof(LudoInput.GreLocations), nameof(LudoInput.GrePositions), nameof(LudoInput.GreMoveable), nameof(LudoInput.GreDouble), nameof(LudoInput.GreInSafe),
                nameof(LudoInput.YelLocations), nameof(LudoInput.YelPositions), nameof(LudoInput.YelMoveable), nameof(LudoInput.YelDouble), nameof(LudoInput.YelInSafe),
                nameof(LudoInput.BluLocations), nameof(LudoInput.BluPositions), nameof(LudoInput.BluMoveable), nameof(LudoInput.BluDouble), nameof(LudoInput.BluInSafe),
                nameof(LudoInput.CurrentPlayer_1), nameof(LudoInput.CurrentPlayer_2), nameof(LudoInput.CurrentPlayer_3), nameof(LudoInput.CurrentPlayer_4), nameof(LudoInput.DiceValue),
                nameof(LudoInput.PlayerScores), nameof(LudoInput.Reward), nameof(LudoInput.SixBonusTurn), nameof(LudoInput.HadExtraTurn), nameof(LudoInput.ExtraTurn), nameof(LudoInput.Done))
                .Append(mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(LudoInput.Action)))
                .Append(mlContext.MulticlassClassification.Trainers.LightGbm(new LightGbmMulticlassTrainer.Options
                {
                    NumberOfIterations = 1000,
                    LearningRate = 0.05,
                    NumberOfLeaves = 64,
                    MinimumExampleCountPerLeaf = 10,
                    Booster = new GradientBooster.Options()
                }))
                .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            Console.WriteLine("⚙️ Training model...");
            var model = pipeline.Fit(split.TrainSet);

            Console.WriteLine("📊 Evaluating...");
            var predictions = model.Transform(split.TestSet);
            var metrics = mlContext.MulticlassClassification.Evaluate(predictions);
            Console.WriteLine($"✅ Accuracy: {metrics.MacroAccuracy:P2}, LogLoss: {metrics.LogLoss:F4}");

            Directory.CreateDirectory(modelOutputDir);
            var outputPath = Path.Combine(modelOutputDir, $"ludo_action_model_{playerIndex}.zip");
            mlContext.Model.Save(model, split.TrainSet.Schema, outputPath);
            Console.WriteLine($"💾 Saved to {outputPath}\n");
        }
    }
}