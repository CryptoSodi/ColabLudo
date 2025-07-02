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
        [VectorType(4)] public float[]? Action { get; set; } //[0,1,0,0] -> R2 moved
        [VectorType(6)] public float[]? DiceValue { get; set; } //[0,0,1,0,0,0] for dice roll of 3    public BaseML StateAfter { get; set; } = default!; // Board state after the action
        public BaseML StateAfter { get; set; } = default!;

        public float Reward { get; set; } // Reward for this action 6
        public float SixBonusTurn { get; set; } // 1 if the extra turn was due to dice == 6
        public float HadExtraTurn { get; set; } // set 1 if this was because of the previous action kill or reached home, otherwise 0
        public float ExtraTurn { get; set; } // Killed or reached home results in 1
        public bool Done { get; set; } // Game End state = false
    }

    public class GameExperienceRecorder
    {
        private readonly List<Experience> experiences = new();
        private int gameIndex { get; set; }
        private int turnIndex { get; set; }
        private BaseML? stateBefore { get; set; }
        private float[]? currentPlayer { get; set; }
        private float[]? action { get; set; }
        private float[]? diceValue { get; set; }  // changed from float to float[] for one-hot vector
        private BaseML? stateAfter { get; set; }        
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
        public void SetGameIndex(int gameIndex)
        {
            this.gameIndex = gameIndex;
        }
        // Set the action taken during this turn (e.g. piece moved)
        public void SetAction(string piece1, string piece2)
        {
            int pi1 = -1;
            int pi2 = -1;
            if (piece1.Contains("1") || piece1.Contains("2") || piece1.Contains("3") || piece1.Contains("4"))
                pi1 = int.Parse(piece1.Replace("red", "").Replace("gre", "").Replace("yel", "").Replace("blu", ""));
            if (piece2.Contains("1") || piece2.Contains("2") || piece2.Contains("3") || piece2.Contains("4"))
                pi2 = int.Parse(piece2.Replace("red", "").Replace("gre", "").Replace("yel", "").Replace("blu", ""));

            var arr = new float[4];
            if(pi1 != -1)
            arr[pi1 - 1] = 1f;
            if (pi2 != -1)
                arr[pi2 - 1] = 1f; // If piece2 is valid, set it as well
            action = arr;
        }

        // Set dice value (should be 1-hot vector of length 6)
        public void SetDiceValue(int diceValue)
        {
            SixBonusTurn = diceValue == 6 ? 1 : 0;
            var arr = new float[6];
            arr[diceValue - 1] = 1f;
            this.diceValue = arr;
        }

        // Set the state after the action
        public void SetStateAfter(BaseML state)
        {
            stateAfter = state;
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
            if (stateBefore == null || currentPlayer == null || action == null || stateAfter == null)
                throw new InvalidOperationException("Incomplete data to save experience.");

            var exp = new Experience
            {
                TurnIndex = turnIndex++,
                StateBefore = stateBefore,
                CurrentPlayer = currentPlayer,
                Action = action,
                DiceValue = diceValue,
                StateAfter = stateAfter,
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
            stateAfter = null;
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
        static int gameIndex = 0;
        public static void ExportToExcel(string filePath, IReadOnlyList<Experience> experiences)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Experiences");

            // Headers including GameIndex
            var headers = new List<string>()
        {
            "GameIndex",
            "TurnIndex",

            // StateBefore - flatten all arrays to columns
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
            // CurrentPlayer (4)
            "CurrentPlayer_1", "CurrentPlayer_2", "CurrentPlayer_3", "CurrentPlayer_4",

            // Action (4)
            "Action_1", "Action_2", "Action_3", "Action_4",

            // DiceValue (6)
            "DiceValue_1", "DiceValue_2", "DiceValue_3", "DiceValue_4", "DiceValue_5", "DiceValue_6",

            // StateAfter (same fields as StateBefore)
            "After_RedLocations_1", "After_RedLocations_2", "After_RedLocations_3", "After_RedLocations_4",
            "After_RedPositions_1", "After_RedPositions_2", "After_RedPositions_3", "After_RedPositions_4",
            "After_RedMoveable_1", "After_RedMoveable_2", "After_RedMoveable_3", "After_RedMoveable_4",
            "After_RedDouble_1", "After_RedDouble_2", "After_RedDouble_3", "After_RedDouble_4",
            "After_RedInSafe_1", "After_RedInSafe_2", "After_RedInSafe_3", "After_RedInSafe_4",

            "After_GreLocations_1", "After_GreLocations_2", "After_GreLocations_3", "After_GreLocations_4",
            "After_GrePositions_1", "After_GrePositions_2", "After_GrePositions_3", "After_GrePositions_4",
            "After_GreMoveable_1", "After_GreMoveable_2", "After_GreMoveable_3", "After_GreMoveable_4",
            "After_GreDouble_1", "After_GreDouble_2", "After_GreDouble_3", "After_GreDouble_4",
            "After_GreInSafe_1", "After_GreInSafe_2", "After_GreInSafe_3", "After_GreInSafe_4",

            "After_YelLocations_1", "After_YelLocations_2", "After_YelLocations_3", "After_YelLocations_4",
            "After_YelPositions_1", "After_YelPositions_2", "After_YelPositions_3", "After_YelPositions_4",
            "After_YelMoveable_1", "After_YelMoveable_2", "After_YelMoveable_3", "After_YelMoveable_4",
            "After_YelDouble_1", "After_YelDouble_2", "After_YelDouble_3", "After_YelDouble_4",
            "After_YelInSafe_1", "After_YelInSafe_2", "After_YelInSafe_3", "After_YelInSafe_4",

            "After_BluLocations_1", "After_BluLocations_2", "After_BluLocations_3", "After_BluLocations_4",
            "After_BluPositions_1", "After_BluPositions_2", "After_BluPositions_3", "After_BluPositions_4",
            "After_BluMoveable_1", "After_BluMoveable_2", "After_BluMoveable_3", "After_BluMoveable_4",
            "After_BluDouble_1", "After_BluDouble_2", "After_BluDouble_3", "After_BluDouble_4",
            "After_BluInSafe_1", "After_BluInSafe_2", "After_BluInSafe_3", "After_BluInSafe_4",

            "After_Red_Score", "After_Gre_Score", "After_Yel_Score", "After_Blu_Score",
            // Other fields
            "Reward",
            "SixBonusTurn",
            "HadExtraTurn",
            "ExtraTurn",
            "Done"
        };

            // Write header row
            for (int i = 0; i < headers.Count; i++)
                worksheet.Cell(1, i + 1).Value = headers[i];

            int row = 2;

            foreach (var exp in experiences)
            {
                int col = 1;

                worksheet.Cell(row, col++).Value = gameIndex;
                worksheet.Cell(row, col++).Value = exp.TurnIndex;

                // Helper local function to write float[] safely (4 elements)
                void WriteVector4(float[]? vector)
                {
                    if (vector == null)
                    {
                        for (int i = 0; i < 4; i++) worksheet.Cell(row, col++).Value = 0f;
                    }
                    else
                    {
                        for (int i = 0; i < 4; i++) worksheet.Cell(row, col++).Value = i < vector.Length ? vector[i] : 0f;
                    }
                }

                // Helper local function to write float[] safely (6 elements)
                void WriteVector6(float[]? vector)
                {
                    if (vector == null)
                    {
                        for (int i = 0; i < 6; i++) worksheet.Cell(row, col++).Value = 0f;
                    }
                    else
                    {
                        for (int i = 0; i < 6; i++) worksheet.Cell(row, col++).Value = i < vector.Length ? vector[i] : 0f;
                    }
                }

                // Write StateBefore fields
                WriteVector4(exp.StateBefore.RedLocations);
                WriteVector4(exp.StateBefore.RedPositions);
                WriteVector4(exp.StateBefore.RedMoveable);
                WriteVector4(exp.StateBefore.RedDouble);
                WriteVector4(exp.StateBefore.RedInSafe);

                WriteVector4(exp.StateBefore.GreLocations);
                WriteVector4(exp.StateBefore.GrePositions);
                WriteVector4(exp.StateBefore.GreMoveable);
                WriteVector4(exp.StateBefore.GreDouble);
                WriteVector4(exp.StateBefore.GreInSafe);

                WriteVector4(exp.StateBefore.YelLocations);
                WriteVector4(exp.StateBefore.YelPositions);
                WriteVector4(exp.StateBefore.YelMoveable);
                WriteVector4(exp.StateBefore.YelDouble);
                WriteVector4(exp.StateBefore.YelInSafe);

                WriteVector4(exp.StateBefore.BluLocations);
                WriteVector4(exp.StateBefore.BluPositions);
                WriteVector4(exp.StateBefore.BluMoveable);
                WriteVector4(exp.StateBefore.BluDouble);
                WriteVector4(exp.StateBefore.BluInSafe);

                WriteVector4(exp.StateBefore.PlayerScores);

                // CurrentPlayer and Action (4 elements each)
                WriteVector4(exp.CurrentPlayer);
                WriteVector4(exp.Action);

                // DiceValue (6 elements)
                WriteVector6(exp.DiceValue);

                // Write StateAfter fields (same as StateBefore)
                WriteVector4(exp.StateAfter.RedLocations);
                WriteVector4(exp.StateAfter.RedPositions);
                WriteVector4(exp.StateAfter.RedMoveable);
                WriteVector4(exp.StateAfter.RedDouble);
                WriteVector4(exp.StateAfter.RedInSafe);

                WriteVector4(exp.StateAfter.GreLocations);
                WriteVector4(exp.StateAfter.GrePositions);
                WriteVector4(exp.StateAfter.GreMoveable);
                WriteVector4(exp.StateAfter.GreDouble);
                WriteVector4(exp.StateAfter.GreInSafe);

                WriteVector4(exp.StateAfter.YelLocations);
                WriteVector4(exp.StateAfter.YelPositions);
                WriteVector4(exp.StateAfter.YelMoveable);
                WriteVector4(exp.StateAfter.YelDouble);
                WriteVector4(exp.StateAfter.YelInSafe);

                WriteVector4(exp.StateAfter.BluLocations);
                WriteVector4(exp.StateAfter.BluPositions);
                WriteVector4(exp.StateAfter.BluMoveable);
                WriteVector4(exp.StateAfter.BluDouble);
                WriteVector4(exp.StateAfter.BluInSafe);

                WriteVector4(exp.StateAfter.PlayerScores);

                // Other simple float and bool fields
                worksheet.Cell(row, col++).Value = exp.Reward;
                worksheet.Cell(row, col++).Value = exp.SixBonusTurn;
                worksheet.Cell(row, col++).Value = exp.HadExtraTurn;
                worksheet.Cell(row, col++).Value = exp.ExtraTurn;
                worksheet.Cell(row, col++).Value = exp.Done ? 1 : 0;

                row++;
            }

            workbook.SaveAs(filePath+ $"game_{gameIndex++}.xlsx");
        }
    }
}
