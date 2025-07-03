namespace AiController
{
    using Microsoft.ML;
    using Microsoft.ML.Data;
    using Microsoft.ML.Trainers.FastTree;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public class MultiActionTrainer
    {
        private static readonly MLContext mlContext = new MLContext(seed: 0);

        /// <summary>
        /// Wraps the feature pipeline and 4 action models.
        /// </summary>
        public class ActionModelWrapper
        {
            private readonly MLContext _mlContext;
            private readonly ITransformer _featurePipeline;
            private readonly ITransformer[] _actionModels;
            private readonly PredictionEngine<TransformedFeatures, RawPrediction>[] _predictionEngines;

            public ActionModelWrapper(MLContext mlContext, ITransformer featurePipeline, ITransformer[] actionModels)
            {
                if (actionModels.Length != 4)
                    throw new ArgumentException("Exactly 4 action models are required.");

                _mlContext = mlContext;
                _featurePipeline = featurePipeline;
                _actionModels = actionModels;

                // Cache prediction engines for faster repeated use
                _predictionEngines = actionModels
                    .Select(model => mlContext.Model.CreatePredictionEngine<TransformedFeatures, RawPrediction>(model))
                    .ToArray();
            }

            /// <summary>
            /// Predicts raw scores for each piece (no thresholding).
            /// </summary>
            public float[] PredictRawScores(LudoExperienceInput input)
            {
                var inputData = _mlContext.Data.LoadFromEnumerable(new[] { input });
                var transformedData = _featurePipeline.Transform(inputData);
                var transformedRow = _mlContext.Data
                    .CreateEnumerable<TransformedFeatures>(transformedData, reuseRowObject: false)
                    .FirstOrDefault();

                if (transformedRow == null)
                    throw new InvalidOperationException("No transformed data available for prediction.");

                var scores = new float[_actionModels.Length];
                for (int i = 0; i < _actionModels.Length; i++)
                {
                    var prediction = _predictionEngines[i].Predict(transformedRow);
                    scores[i] = prediction.Score;
                }
                return scores;
            }

            /// <summary>
            /// Predicts which pieces should be moved based on a confidence threshold.
            /// Can return multiple pieces (e.g. for a double move).
            /// </summary>
            public List<int> PredictActions(LudoExperienceInput input, float threshold = 0.5f)
            {
                float[] scores = PredictRawScores(input);
                var selectedActions = new List<int>();

                for (int i = 0; i < scores.Length; i++)
                {
                    if (scores[i] >= threshold)
                    {
                        selectedActions.Add(i); // i = piece index
                    }
                }

                return selectedActions;
            }

            /// <summary>
            /// Loads a saved feature pipeline and 4 action models.
            /// </summary>
            public static ActionModelWrapper Load(MLContext mlContext, string featurePipelinePath, string[] actionModelPaths)
            {
                if (actionModelPaths.Length != 4)
                    throw new ArgumentException("Exactly 4 paths for action models are required.");

                var featurePipeline = mlContext.Model.Load(featurePipelinePath, out var _);
                var actionModels = new ITransformer[4];
                for (int i = 0; i < 4; i++)
                {
                    actionModels[i] = mlContext.Model.Load(actionModelPaths[i], out var _);
                }
                return new ActionModelWrapper(mlContext, featurePipeline, actionModels);
            }

            private class TransformedFeatures
            {
                [VectorType]
                public float[] Features { get; set; }
            }

            private class RawPrediction
            {
                public float Score { get; set; }
            }
        }


        /// <summary>
        /// Trains AI models for predicting Ludo piece moves.
        /// </summary>
        public static async Task<ActionModelWrapper> TrainAi()
        {
            Console.WriteLine("📂 Loading data...");

            var combinedData = mlContext.Data.LoadFromTextFile<LudoExperienceInput>(
                path: Path.Combine(@"D:\LudoAiData\__temp_csv2", "*.csv"),
                hasHeader: true,
                separatorChar: ',');

            var split = mlContext.Data.TrainTestSplit(combinedData, testFraction: 0.2);

            // Step 1: Define the feature engineering pipeline
            var featurePipeline = mlContext.Transforms.Concatenate(
                    "Features",
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
                    nameof(LudoExperienceInput.DiceValue)
                )
                .Append(mlContext.Transforms.NormalizeMinMax("Features"))
                .AppendCacheCheckpoint(mlContext);

            Console.WriteLine("⚙️ Fitting feature pipeline...");
            var fittedFeaturePipeline = featurePipeline.Fit(split.TrainSet);

            var actionModels = new List<ITransformer>();

            for (int i = 0; i < 4; i++)
            {
                Console.WriteLine($"🧠 Training Action[{i}]...");

                var binaryPipeline = mlContext.Transforms.Conversion
                    .ConvertType(outputColumnName: "Label", inputColumnName: $"Action_{i}", outputKind: DataKind.Boolean)
                    .Append(featurePipeline)
                    .Append(mlContext.BinaryClassification.Trainers.FastTree(new FastTreeBinaryTrainer.Options
                    {
                        NumberOfLeaves = 128,
                        NumberOfTrees = 500,
                        LearningRate = 0.05,
                        MinimumExampleCountPerLeaf = 10
                    }));

                var binaryModel = binaryPipeline.Fit(split.TrainSet);
                actionModels.Add(binaryModel);

                var modelPath = $"ludo_action_model_{i}.zip";
                mlContext.Model.Save(binaryModel, split.TrainSet.Schema, modelPath);
                Console.WriteLine($"✅ Saved Action[{i}] model → {modelPath}");

                // 🧪 Evaluate on test data
                Console.WriteLine($"📊 Evaluating Action[{i}] on test set...");
                var testDataWithLabel = mlContext.Transforms.Conversion
                    .ConvertType("Label", $"Action_{i}", DataKind.Boolean)
                    .Append(featurePipeline)
                    .Fit(split.TestSet)
                    .Transform(split.TestSet);

                var predictions = binaryModel.Transform(split.TestSet);
                var metrics = mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: "Label");

                Console.WriteLine($"📈 Action[{i}] Metrics:");
                Console.WriteLine($"   Accuracy     : {metrics.Accuracy:P2}");
                Console.WriteLine($"   AUC          : {metrics.AreaUnderRocCurve:P2}");
                Console.WriteLine($"   F1 Score     : {metrics.F1Score:P2}");
                Console.WriteLine($"   Precision    : {metrics.PositivePrecision:P2}");
                Console.WriteLine($"   Recall       : {metrics.PositiveRecall:P2}");
            }


            // Step 4: Save the fitted feature pipeline separately for use in inference
            var featurePipelinePath = "ludo_feature_pipeline.zip";
            mlContext.Model.Save(fittedFeaturePipeline, split.TrainSet.Schema, featurePipelinePath);
            Console.WriteLine($"✅ Saved feature pipeline → {featurePipelinePath}");

            Console.WriteLine("🎉 Training complete!");

            return new ActionModelWrapper(mlContext, fittedFeaturePipeline, actionModels.ToArray());
        }

    }

    public class LudoExperienceInput
    {
        [LoadColumn(0)] public float GameIndex { get; set; }
        [LoadColumn(1)] public float TurnIndex { get; set; }

        [LoadColumn(2, 5), VectorType(4)] public float[] RedLocations { get; set; }
        [LoadColumn(6, 9), VectorType(4)] public float[] RedPositions { get; set; }
        [LoadColumn(10, 13), VectorType(4)] public float[] RedMoveable { get; set; }
        [LoadColumn(14, 17), VectorType(4)] public float[] RedDouble { get; set; }
        [LoadColumn(18, 21), VectorType(4)] public float[] RedInSafe { get; set; }

        [LoadColumn(22, 25), VectorType(4)] public float[] GreLocations { get; set; }
        [LoadColumn(26, 29), VectorType(4)] public float[] GrePositions { get; set; }
        [LoadColumn(30, 33), VectorType(4)] public float[] GreMoveable { get; set; }
        [LoadColumn(34, 37), VectorType(4)] public float[] GreDouble { get; set; }
        [LoadColumn(38, 41), VectorType(4)] public float[] GreInSafe { get; set; }

        [LoadColumn(42, 45), VectorType(4)] public float[] YelLocations { get; set; }
        [LoadColumn(46, 49), VectorType(4)] public float[] YelPositions { get; set; }
        [LoadColumn(50, 53), VectorType(4)] public float[] YelMoveable { get; set; }
        [LoadColumn(54, 57), VectorType(4)] public float[] YelDouble { get; set; }
        [LoadColumn(58, 61), VectorType(4)] public float[] YelInSafe { get; set; }

        [LoadColumn(62, 65), VectorType(4)] public float[] BluLocations { get; set; }
        [LoadColumn(66, 69), VectorType(4)] public float[] BluPositions { get; set; }
        [LoadColumn(70, 73), VectorType(4)] public float[] BluMoveable { get; set; }
        [LoadColumn(74, 77), VectorType(4)] public float[] BluDouble { get; set; }
        [LoadColumn(78, 81), VectorType(4)] public float[] BluInSafe { get; set; }

        [LoadColumn(82, 85), VectorType(4)] public float[] PlayerScores { get; set; }
        [LoadColumn(86, 89), VectorType(4)] public float[] CurrentPlayer { get; set; }

        // ✅ Split action vector into 4 distinct columns
        [LoadColumn(90)] public float Action_0 { get; set; }
        [LoadColumn(91)] public float Action_1 { get; set; }
        [LoadColumn(92)] public float Action_2 { get; set; }
        [LoadColumn(93)] public float Action_3 { get; set; }

        [LoadColumn(94, 99), VectorType(6)] public float[] DiceValue { get; set; }

        // Other extra info (not used in training but available)
        [LoadColumn(184)] public float Reward { get; set; }
        [LoadColumn(188)] public bool Done { get; set; }
    }

}
