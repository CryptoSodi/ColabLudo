using ClosedXML.Excel;
using Microsoft.ML.Data;

namespace SharedCode.CoreEngine
{
    public class BaseML
    {
        public BaseML(Engine engine)
        {
            PlayerScores = new float[4];

            Player pRed = engine.EngineHelper.getPlayer("red");
            Player pGre = engine.EngineHelper.getPlayer("green");
            Player pYel = engine.EngineHelper.getPlayer("yellow");
            Player pBlu = engine.EngineHelper.getPlayer("blue");

            RedLocations = new float[4];
            RedPositions = new float[4];
            RedMoveable = new float[4];
            RedDouble = new float[4];
            RedInSafe = new float[4];

            GreLocations = new float[4];
            GrePositions = new float[4];
            GreMoveable = new float[4];
            GreDouble = new float[4];
            GreInSafe = new float[4];

            YelLocations = new float[4];
            YelPositions = new float[4];
            YelMoveable = new float[4];
            YelDouble = new float[4];
            YelInSafe = new float[4];

            BluLocations = new float[4];
            BluPositions = new float[4];
            BluMoveable = new float[4];
            BluDouble = new float[4];
            BluInSafe = new float[4];

            for (int i = 0; i < 4; i++)
            {
                if (pRed != null)
                {
                    Piece? piece = pRed.Pieces.FirstOrDefault(p => p.Name == ($"red{i + 1}"));
                    if (piece != null)
                    {
                        RedLocations[i] = piece.Location;
                        RedPositions[i] = piece.Position;
                        RedMoveable[i] = piece.Moveable ? 1 : 0;
                        RedDouble[i] = piece.DoubleMoveable ? 1 : 0;
                        RedInSafe[i] = engine.EngineHelper.safeZone.Contains(piece.Position) ? 1 : 0;
                    }
                    else
                    {
                        piece = pRed.removedPieces.FirstOrDefault(p => p.Name == ($"red{i + 1}"));
                        RedLocations[i] = piece.Location;
                        RedPositions[i] = piece.Position;
                        RedMoveable[i] = 0;
                        RedDouble[i] = 0;
                        RedInSafe[i] = engine.EngineHelper.safeZone.Contains(piece.Position) ? 1 : 0;
                    }
                    PlayerScores[0] = pRed.Pieces.Sum(p => p.Location + (p.Location == 57 ? 57 : 0)) + pRed.Score;
                }
                else
                {
                }
                if (pGre != null)
                {
                    Piece? piece = pGre.Pieces.FirstOrDefault(p => p.Name == ($"gre{i + 1}"));
                    if (piece != null)
                    {
                        GreLocations[i] = piece.Location;
                        GrePositions[i] = piece.Position;
                        GreMoveable[i] = piece.Moveable ? 1 : 0;
                        GreDouble[i] = piece.DoubleMoveable ? 1 : 0;
                        GreInSafe[i] = engine.EngineHelper.safeZone.Contains(piece.Position) ? 1 : 0;
                    }
                    else
                    {
                        piece = pGre.removedPieces.FirstOrDefault(p => p.Name == ($"gre{i + 1}"));
                        GreLocations[i] = piece.Location;
                        GrePositions[i] = piece.Position;
                        GreMoveable[i] = 0;
                        GreDouble[i] = 0;
                        GreInSafe[i] = engine.EngineHelper.safeZone.Contains(piece.Position) ? 1 : 0;
                    }
                    PlayerScores[1] = pGre.Pieces.Sum(p => p.Location + (p.Location == 57 ? 57 : 0)) + pGre.Score;
                }
                else
                {
                }
                if (pYel != null)
                {
                    Piece? piece = pYel.Pieces.FirstOrDefault(p => p.Name == ($"yel{i + 1}"));
                    if (piece != null)
                    {
                        YelLocations[i] = piece.Location;
                        YelPositions[i] = piece.Position;
                        YelMoveable[i] = piece.Moveable ? 1 : 0;
                        YelDouble[i] = piece.DoubleMoveable ? 1 : 0;
                        YelInSafe[i] = engine.EngineHelper.safeZone.Contains(piece.Position) ? 1 : 0;
                    }
                    else
                    {
                        piece = pYel.removedPieces.FirstOrDefault(p => p.Name == ($"yel{i + 1}"));
                        YelLocations[i] = piece.Location;
                        YelPositions[i] = piece.Position;
                        YelMoveable[i] = 0;
                        YelDouble[i] = 0;
                        YelInSafe[i] = engine.EngineHelper.safeZone.Contains(piece.Position) ? 1 : 0;
                    }
                    PlayerScores[2] = pYel.Pieces.Sum(p => p.Location + (p.Location == 57 ? 57 : 0)) + pYel.Score;
                }
                else
                {
                }
                if (pBlu != null)
                {
                    Piece? piece = pBlu.Pieces.FirstOrDefault(p => p.Name == ($"blu{i + 1}"));
                    if (piece != null)
                    {
                        BluLocations[i] = piece.Location;
                        BluPositions[i] = piece.Position;
                        BluMoveable[i] = piece.Moveable ? 1 : 0;
                        BluDouble[i] = piece.DoubleMoveable ? 1 : 0;
                        BluInSafe[i] = engine.EngineHelper.safeZone.Contains(piece.Position) ? 1 : 0;
                    }
                    else
                    {
                        piece = pBlu.removedPieces.FirstOrDefault(p => p.Name == ($"blu{i + 1}"));
                        BluLocations[i] = piece.Location;
                        BluPositions[i] = piece.Position;
                        BluMoveable[i] = 0;
                        BluDouble[i] = 0;
                        BluInSafe[i] = engine.EngineHelper.safeZone.Contains(piece.Position) ? 1 : 0;
                    }
                    PlayerScores[3] = pBlu.Pieces.Sum(p => p.Location + (p.Location == 57 ? 57 : 0)) + pBlu.Score;
                }
                else
                {
                }
            }
        }

        [VectorType(4)] public float[]? RedLocations { get; set; }// [10,20,-1,57] Location on the board (distance towards goal)
        [VectorType(4)] public float[]? RedPositions { get; set; } // [ph0,ph1,p20,rg3] Logical positions (ph = in home, p = on board, rg = at goal)
        [VectorType(4)] public float[]? RedMoveable { get; set; }// [1,1,0,0] Indicates if each piece is currently moveable
        [VectorType(4)] public float[]? RedDouble { get; set; }// [1,1,0,0] Indicates which pieces can double-move
        [VectorType(4)] public float[]? RedInSafe { get; set; }// [1,1,0,0] Whether each piece is in a safe zone

        [VectorType(4)] public float[]? GreLocations { get; set; }
        [VectorType(4)] public float[]? GrePositions { get; set; }
        [VectorType(4)] public float[]? GreMoveable { get; set; }
        [VectorType(4)] public float[]? GreDouble { get; set; }
        [VectorType(4)] public float[]? GreInSafe { get; set; }

        [VectorType(4)] public float[]? YelLocations { get; set; }
        [VectorType(4)] public float[]? YelPositions { get; set; }
        [VectorType(4)] public float[]? YelMoveable { get; set; }
        [VectorType(4)] public float[]? YelDouble { get; set; }
        [VectorType(4)] public float[]? YelInSafe { get; set; }

        [VectorType(4)] public float[]? BluLocations { get; set; }
        [VectorType(4)] public float[]? BluPositions { get; set; }
        [VectorType(4)] public float[]? BluMoveable { get; set; }
        [VectorType(4)] public float[]? BluDouble { get; set; }
        [VectorType(4)] public float[]? BluInSafe { get; set; }

        // Optional: total score of each player
        [VectorType(4)] public float[]? PlayerScores { get; set; } // Sum of piece locations + bonuses
    }

    public class Experience
    {
        public int TurnIndex { get; set; } // Sequential turn index for the game
        public BaseML StateBefore { get; set; } = default!; // Board state before the action
        [VectorType(4)] public float[]? CurrentPlayer { get; set; } //[1,0,0,0] for Red
        public float? Action { get; set; } // Now a string label like "R1" or "R1+R2"
        public string? ActionString { get; set; } // Now a string label like "R1" or "R1+R2"
        [VectorType(6)] public float[]? DiceValue { get; set; } //[0,0,1,0,0,0] for dice roll of 3    public BaseML StateAfter { get; set; } = default!; // Board state after the action

        public float Reward { get; set; } // Reward for this action 6
        public float SixBonusTurn { get; set; } // 1 if the extra turn was due to dice == 6
        public float HadExtraTurn { get; set; } // set 1 if this was because of the previous action kill or reached home, otherwise 0
        public float ExtraTurn { get; set; } // Killed or reached home results in 1
        public bool Done { get; set; } // Game End state = false
    }

    public class GameExperienceRecorder
    {
        private static readonly Dictionary<string, float> ActionMap = GenerateActionMap();
        private static Dictionary<string, float> GenerateActionMap()
        {
            var dict = new Dictionary<string, float>();
            string[] colors = { "red", "gre", "yel", "blu" };
            int counter = 1;

            // Single-piece actions
            foreach (var color in colors)
                for (int i = 1; i <= 4; i++)
                    dict[$"{color}{i}"] = counter++;

            // Double-piece actions
            var pieces = dict.Keys.ToList();
            foreach (var first in pieces)
            {
                foreach (var second in pieces)
                {
                    if (first == second) continue; // Skip same piece
                    var pair = string.Join("+", new[] { first, second }.OrderBy(x => x));
                    if (!dict.ContainsKey(pair))
                        dict[pair] = counter++;
                }
            }

            return dict;
        }

        private readonly List<Experience> experiences = new();
        private int gameIndex { get; set; }
        private int turnIndex { get; set; }
        private BaseML? stateBefore { get; set; }
        private float[]? currentPlayer { get; set; }
        public float? action { get; set; } // Now a string label like "R1" or "R1+R2"
        public string? actionString { get; set; } // Now a string label like "R1" or "R1+R2"
        private float[]? diceValue { get; set; }  // changed from float to float[] for one-hot vector
        public float reward { get; set; }
        private float SixBonusTurn { get; set; }
        private float hadExtraTurn { get; set; }
        private float extraTurn { get; set; }
        private bool done { get; set; }

        // Set initial state before action
        public void SetStateBefore(BaseML state, string currentPlayerVector, int diceValue)
        {
            reward = 0;
            stateBefore = state;
            SetDiceValue(diceValue);
            var arr = new float[4];
            switch (currentPlayerVector)
            {
                case "red":
                    arr[0] = 1f;
                    break;
                case "green":
                    arr[1] = 1f;
                    break;
                case "yellow":
                    arr[2] = 1f;
                    break;
                case "blue":
                    arr[3] = 1f;
                    break;
            }
            currentPlayer = arr;
            stateBefore = state;
        }
        // Set the action taken during this turn (e.g. piece moved)
        public void SetAction(string piece1, string piece2)
        {
            string actionstring = piece1;
            if (!string.IsNullOrEmpty(piece2))
                actionstring += "+" + piece2;
            var sortedAction = string.Join("+", actionstring.Split('+').OrderBy(x => x));

            // Lookup in the map
            if (!ActionMap.TryGetValue(sortedAction, out float actionFinal))
                throw new InvalidOperationException($"Unknown action: {sortedAction}");

            this.action = actionFinal;       // Save float encoding
            this.actionString = sortedAction;   // Save float encoding
        }
      

        // Set dice value (should be 1-hot vector of length 6)
        public void SetDiceValue(int diceValue)
        {
            SixBonusTurn = diceValue == 6 ? 1 : 0;
            var arr = new float[6];
            arr[diceValue - 1] = 1f;
            this.diceValue = arr;
        }

        // Set the reward gained for this action
        public void SetReward(float reward)
        {
            this.reward += reward;
        }

        // Indicate if this turn was due to an extra turn from previous action
        public void SetHadExtraTurn(float hadExtraTurn)
        {
            this.hadExtraTurn = hadExtraTurn;
        }

        // Indicate if this action resulted in an extra turn
        public void SetExtraTurn(float extraTurn)
        {
            this.extraTurn = extraTurn;
        }

        // Set if game is finished
        public void SetDone(bool done)
        {
            this.done = done;
        }
        // Call this at the end of the turn to save the experience record
        public void SaveExperience()
        {
            if (stateBefore == null || currentPlayer == null || action == null )
                throw new InvalidOperationException("Incomplete data to save experience.");

            var exp = new Experience
            {
                TurnIndex = turnIndex++,
                StateBefore = stateBefore,
                CurrentPlayer = currentPlayer,
                Action = action,
                ActionString = actionString,
                DiceValue = diceValue,
                Reward = reward,
                SixBonusTurn = SixBonusTurn,
                HadExtraTurn = hadExtraTurn,
                ExtraTurn = extraTurn,
                Done = done,
            };

            experiences.Add(exp);

            // Reset temporary values for next turn
            stateBefore = null;
            currentPlayer = null;
            action = null;
            actionString = null;
            reward = 0;
            SixBonusTurn = 0;
            hadExtraTurn = 0;
            extraTurn = 0;
            done = false;
        }

        // Retrieve all recorded experiences
        public IReadOnlyList<Experience> GetAllExperiences() => experiences.AsReadOnly();

        // Clear all stored experiences if needed
        public void Clear()
        {
            experiences.Clear();
        }
    }
    //Reward = steps_moved
    //   + (kill? +6 : 0)
    //   + (double_safe? +2 :
    //       entered_safe_zone? +2 : 0)
    //   + (reached_goal? 57 : 0)
    public class GameExperienceExporter
    {
        public static int gameIndex = 0;
        public static void ExportToCsv(string filePath, IReadOnlyList<Experience> experiences)
        {
            using var writer = new StreamWriter(filePath + $"game_{gameIndex}.csv");

            // Headers
            var headers = new List<string>()
    {
        "GameIndex", "TurnIndex", "ActionString", "Action",
        "RedLocations_1", "RedLocations_2", "RedLocations_3", "RedLocations_4",
        "RedPositions_1", "RedPositions_2", "RedPositions_3", "RedPositions_4",
        "RedMoveable_1", "RedMoveable_2", "RedMoveable_3", "RedMoveable_4",
        "RedDouble_1", "RedDouble_2", "RedDouble_3", "RedDouble_4",
        "RedInSafe_1", "RedInSafe_2", "RedInSafe_3", "RedInSafe_4",
        "GreLocations_1", "GreLocations_2", "GreLocations_3", "GreLocations_4",
        "GrePositions_1", "GrePositions_2", "GrePositions_3", "GrePositions_4",
        "GreMoveable_1", "GreMoveable_2", "GreMoveable_3", "GreMoveable_4",
        "GreDouble_1", "GreDouble_2", "GreDouble_3", "GreDouble_4",
        "GreInSafe_1", "GreInSafe_2", "GreInSafe_3", "GreInSafe_4",
        "YelLocations_1", "YelLocations_2", "YelLocations_3", "YelLocations_4",
        "YelPositions_1", "YelPositions_2", "YelPositions_3", "YelPositions_4",
        "YelMoveable_1", "YelMoveable_2", "YelMoveable_3", "YelMoveable_4",
        "YelDouble_1", "YelDouble_2", "YelDouble_3", "YelDouble_4",
        "YelInSafe_1", "YelInSafe_2", "YelInSafe_3", "YelInSafe_4",
        "BluLocations_1", "BluLocations_2", "BluLocations_3", "BluLocations_4",
        "BluPositions_1", "BluPositions_2", "BluPositions_3", "BluPositions_4",
        "BluMoveable_1", "BluMoveable_2", "BluMoveable_3", "BluMoveable_4",
        "BluDouble_1", "BluDouble_2", "BluDouble_3", "BluDouble_4",
        "BluInSafe_1", "BluInSafe_2", "BluInSafe_3", "BluInSafe_4",
        "Red_Score", "Gre_Score", "Yel_Score", "Blu_Score",
        "CurrentPlayer_1", "CurrentPlayer_2", "CurrentPlayer_3", "CurrentPlayer_4",
        "DiceValue_1", "DiceValue_2", "DiceValue_3", "DiceValue_4", "DiceValue_5", "DiceValue_6",
        "Reward", "SixBonusTurn", "HadExtraTurn", "ExtraTurn", "Done"
    };
            writer.WriteLine(string.Join(",", headers));

            foreach (var exp in experiences)
            {
                var row = new List<string>();

                row.Add(gameIndex.ToString());
                row.Add(exp.TurnIndex.ToString());
                row.Add(exp.ActionString ?? "");
                row.Add(exp.Action?.ToString() ?? "0");

                void WriteVector(IEnumerable<float>? vector, int expectedLength)
                {
                    if (vector == null)
                    {
                        row.AddRange(Enumerable.Repeat("0", expectedLength));
                    }
                    else
                    {
                        row.AddRange(vector.Select(v => v.ToString("0.######")));
                    }
                }

                // Write vectors
                WriteVector(exp.StateBefore.RedLocations, 4);
                WriteVector(exp.StateBefore.RedPositions, 4);
                WriteVector(exp.StateBefore.RedMoveable, 4);
                WriteVector(exp.StateBefore.RedDouble, 4);
                WriteVector(exp.StateBefore.RedInSafe, 4);

                WriteVector(exp.StateBefore.GreLocations, 4);
                WriteVector(exp.StateBefore.GrePositions, 4);
                WriteVector(exp.StateBefore.GreMoveable, 4);
                WriteVector(exp.StateBefore.GreDouble, 4);
                WriteVector(exp.StateBefore.GreInSafe, 4);

                WriteVector(exp.StateBefore.YelLocations, 4);
                WriteVector(exp.StateBefore.YelPositions, 4);
                WriteVector(exp.StateBefore.YelMoveable, 4);
                WriteVector(exp.StateBefore.YelDouble, 4);
                WriteVector(exp.StateBefore.YelInSafe, 4);

                WriteVector(exp.StateBefore.BluLocations, 4);
                WriteVector(exp.StateBefore.BluPositions, 4);
                WriteVector(exp.StateBefore.BluMoveable, 4);
                WriteVector(exp.StateBefore.BluDouble, 4);
                WriteVector(exp.StateBefore.BluInSafe, 4);

                WriteVector(exp.StateBefore.PlayerScores, 4);
                WriteVector(exp.CurrentPlayer, 4);
                WriteVector(exp.DiceValue, 6);

                row.Add(exp.Reward.ToString("0.######"));
                row.Add(exp.SixBonusTurn.ToString("0"));
                row.Add(exp.HadExtraTurn.ToString("0"));
                row.Add(exp.ExtraTurn.ToString("0"));
                row.Add(exp.Done ? "1" : "0");

                writer.WriteLine(string.Join(",", row));
            }
        }

    }
}