using SharedCode.ControlView;
using SharedCode.CoreEngine;
namespace SharedCode
{
    public partial class MainPage : ContentPage
    {
        String playerColor = "Red";
        String gameType = "4";
        String gameMode = "";
        public PlayerSeat RedPlayerSeat;
        public PlayerSeat GreenPlayerSeat;
        public PlayerSeat YellowPlayerSeat;
        public PlayerSeat BluePlayerSeat;
        Gui gui;
        public Engine engine;
        List<PlayerDto>? seats = new List<PlayerDto>();
        public PlayerSeat GetPlayerSeat(string seatColor)
        {
            if (seatColor.ToLower() == "red")
                return gui.red;
            else if (seatColor.ToLower() == "green")
                return gui.green;
            else if (seatColor.ToLower() == "yellow")
                return gui.yellow;
            else
                return gui.blue;
        }
        public MainPage()
        {
            InitializeComponent();
            this.gameMode = gameMode;
            RedPlayerSeat = new PlayerSeat("red")
            {
                PlayerBG = "red_container.png",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.End
            };
            // Create GreenPlayerSeat
            GreenPlayerSeat = new PlayerSeat("green")
            {
                PlayerBG = "green_container.png",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.End
            };
            // Create YellowPlayerSeat
            YellowPlayerSeat = new PlayerSeat("yellow")
            {
                PlayerBG = "yellow_container.png",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.End
            };
            // Create BluePlayerSeat
            BluePlayerSeat = new PlayerSeat("blue")
            {
                PlayerBG = "blue_container.png",
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.End
            };
            gui = new Gui(red1, red2, red3, red4, gre1, gre2, gre3, gre4, blu1, blu2, blu3, blu4, yel1, yel2, yel3, yel4, LockHome1, LockHome2, LockHome3, LockHome4, RedPlayerSeat, GreenPlayerSeat, YellowPlayerSeat, BluePlayerSeat);

            Row1.Children.Clear();
            Row2.Children.Clear();

            switch (gameType)
            {
                case "2":
                    switch (playerColor)
                    {
                        case "Red":
                            Row1.Children.Add(YellowPlayerSeat);
                            Row2.Children.Add(RedPlayerSeat);
                            break;
                        case "Yellow":
                            Row2.Children.Add(YellowPlayerSeat);
                            Row1.Children.Add(RedPlayerSeat);
                            break;
                        case "Green":
                            Row1.Children.Add(BluePlayerSeat);
                            Row2.Children.Add(GreenPlayerSeat);
                            break;
                        case "Blue":
                            Row2.Children.Add(BluePlayerSeat);
                            Row1.Children.Add(GreenPlayerSeat);
                            break;
                    }
                    break;
                case "3":
                    switch (playerColor)
                    {
                        case "Red":
                            Row1.Children.Add(GreenPlayerSeat);
                            Row1.Children.Add(YellowPlayerSeat);
                            Row2.Children.Add(RedPlayerSeat);
                            break;
                        case "Yellow":
                            Row1.Children.Add(BluePlayerSeat);
                            Row1.Children.Add(RedPlayerSeat);
                            Row2.Children.Add(YellowPlayerSeat);
                            break;
                        case "Green":
                            Row1.Children.Add(YellowPlayerSeat);
                            Row1.Children.Add(BluePlayerSeat);
                            Row2.Children.Add(GreenPlayerSeat);
                            break;
                        case "Blue":
                            Row1.Children.Add(RedPlayerSeat);
                            Row1.Children.Add(GreenPlayerSeat);
                            Row2.Children.Add(BluePlayerSeat);
                            break;
                    }
                    break;
                case "4":
                    switch (playerColor)
                    {
                        case "Red":
                            Row2.Children.Add(RedPlayerSeat);
                            Row2.Children.Add(BluePlayerSeat);

                            Row1.Children.Add(GreenPlayerSeat);
                            Row1.Children.Add(YellowPlayerSeat);
                            break;
                        case "Green":
                            Row2.Children.Add(GreenPlayerSeat);
                            Row2.Children.Add(RedPlayerSeat);

                            Row1.Children.Add(YellowPlayerSeat);
                            Row1.Children.Add(BluePlayerSeat);
                            break;
                        case "Yellow":
                            Row1.Children.Add(BluePlayerSeat);
                            Row1.Children.Add(RedPlayerSeat);

                            Row2.Children.Add(YellowPlayerSeat);
                            Row2.Children.Add(GreenPlayerSeat);
                            break;
                        case "Blue":
                            Row2.Children.Add(BluePlayerSeat);
                            Row2.Children.Add(YellowPlayerSeat);

                            Row1.Children.Add(RedPlayerSeat);
                            Row1.Children.Add(GreenPlayerSeat);
                            break;
                    }
                    break;
                case "22":
                    switch (playerColor)
                    {
                        case "Red":
                            Row2.Children.Add(RedPlayerSeat);
                            Row2.Children.Add(BluePlayerSeat);

                            Row1.Children.Add(GreenPlayerSeat);
                            Row1.Children.Add(YellowPlayerSeat);
                            break;
                        case "Green":
                            Row2.Children.Add(GreenPlayerSeat);
                            Row2.Children.Add(RedPlayerSeat);

                            Row1.Children.Add(YellowPlayerSeat);
                            Row1.Children.Add(BluePlayerSeat);
                            break;
                        case "Yellow":
                            Row1.Children.Add(BluePlayerSeat);
                            Row1.Children.Add(RedPlayerSeat);

                            Row2.Children.Add(YellowPlayerSeat);
                            Row2.Children.Add(GreenPlayerSeat);
                            break;
                        case "Blue":
                            Row2.Children.Add(BluePlayerSeat);
                            Row2.Children.Add(YellowPlayerSeat);

                            Row1.Children.Add(GreenPlayerSeat);
                            Row1.Children.Add(RedPlayerSeat);
                            break;
                    }
                    break;
            }
            Alayout.Remove(gui.red1);
            Alayout.Remove(gui.red2);
            Alayout.Remove(gui.red3);
            Alayout.Remove(gui.red4);
            Alayout.Remove(gui.gre1);
            Alayout.Remove(gui.gre2);
            Alayout.Remove(gui.gre3);
            Alayout.Remove(gui.gre4);
            Alayout.Remove(gui.yel1);
            Alayout.Remove(gui.yel2);
            Alayout.Remove(gui.yel3);
            Alayout.Remove(gui.yel4);
            Alayout.Remove(gui.blu1);
            Alayout.Remove(gui.blu2);
            Alayout.Remove(gui.blu3);
            Alayout.Remove(gui.blu4);

            Alayout.Remove(gui.LockHome1);
            Alayout.Remove(gui.LockHome2);
            Alayout.Remove(gui.LockHome3);
            Alayout.Remove(gui.LockHome4);

            var colors = new[] { ("Red", gui.red), ("Green", gui.green), ("Yellow", gui.yellow), ("Blue", gui.blue) };

            foreach (var (color, seat) in colors)
                seat.initAuto(this, $"AI {Array.IndexOf(colors, (color, seat)) + 1}", "player.png", "ShowAuto", true);

            engine = new Engine("AI", gameType, gameType == "22" ? "4" : gameType, playerColor, "");

            gui.red.engineHelper = engine.EngineHelper;
            gui.green.engineHelper = engine.EngineHelper;
            gui.yellow.engineHelper = engine.EngineHelper;
            gui.blue.engineHelper = engine.EngineHelper;


            int rotation = engine.EngineHelper.SetRotation(this.playerColor);
            Glayout?.RotateTo(rotation);

            foreach (var player in engine.EngineHelper.players)
                foreach (var piece in player.Pieces)
                    Alayout.Add(gui.getPieceToken(piece));

            SetHomeBlock(gui.LockHome1, "red");
            SetHomeBlock(gui.LockHome2, "green");
            SetHomeBlock(gui.LockHome3, "yellow");
            SetHomeBlock(gui.LockHome4, "blue");

            Alayout.SizeChanged += (sender, e) => { Pupulate(rotation); };

            RedPlayerSeat.reset();
            GreenPlayerSeat.reset();
            YellowPlayerSeat.reset();
            BluePlayerSeat.reset();

            foreach (var player in engine.EngineHelper.players)
            {
                var playerp = GetPlayerSeat(player.Color);
                seats.Add(new PlayerDto
                {
                    PlayerColor = playerp.seatColor,
                    PlayerName = playerp.PlayerName,
                    PlayerPicture = playerp.PlayerImageSource
                });
            }


            engine.StopDice += new Engine.CallbackEventHandler(StopDice);
            engine.AnimateDice += new Engine.Callback_AnimateDice_EventHandler(AnimateDice);
            engine.StartProgressAnimation += new Engine.CallbackEventHandlerStartProgressAnimation(StartProgressAnimation);
            engine.StopProgressAnimation += new Engine.CallbackEventHandlerStopProgressAnimation(StopProgressAnimation);
            engine.RelocateAsync += new Engine.CallbackEventHandlerRelocateAsync(RelocateAsync);
            engine.ShowResults += new Engine.CallbackEventHandlerShowResults(ShowResults);
            engine.PlayerLeftSeat += new Engine.CallbackEventHandlerPlayerLeft(PlayerLeftSeat);

            StartProgressAnimation(engine.EngineHelper.currentPlayer.Color);
        }
        private void SetHomeBlock(Token lockHome, string color)
        {
            var player = engine.EngineHelper.getPlayer(color);
            // If player is null OR the player exists and cannot enter the goal, add the block.
            if (player == null || player?.CanEnterGoal == false)
            {
                if (!Alayout.Contains(lockHome))
                {
                    Alayout.Add(lockHome);
                }
                AbsoluteLayout.SetLayoutBounds(lockHome, new Rect(0, 0, (Alayout.Width / 15) - 6, (Alayout.Height / 15) - 6));
                string PB = color.Substring(0, 1) + 51;
                double x = engine.EngineHelper.originalPath[PB][1] * (Alayout.Width / 15);
                double y = engine.EngineHelper.originalPath[PB][0] * (Alayout.Height / 15);
                _ = lockHome.TranslateTo(x + 3, y + 3, 10, Easing.CubicIn);
            }
            else
            {
                if (Alayout.Contains(lockHome))
                {
                    lockHome.TranslateTo(-300, -300, 10, Easing.CubicIn);
                    Alayout.Remove(lockHome);
                }
            }
        }
        public void Pupulate(int rotation)
        {
            for (int i = 0; i < engine.EngineHelper.players.Count; i++)
                for (int j = 0; j < engine.EngineHelper.players[i].Pieces.Count; j++)
                {
                    gui.getPieceToken(engine.EngineHelper.players[i].Pieces[j]).RotateTo(-rotation);
                    AbsoluteLayout.SetLayoutBounds(gui.getPieceToken(engine.EngineHelper.players[i].Pieces[j]), new Rect(0, 0, (Alayout.Width / 15), (Alayout.Height / 15)));
                    List<Piece> pieces = new List<Piece>();
                    pieces.Add(engine.EngineHelper.players[i].Pieces[j]);
                    _ = RelocateAsync(pieces, engine.EngineHelper.players[i].Pieces[j], "");
                }

            SetHomeBlock(gui.LockHome1, "red");
            SetHomeBlock(gui.LockHome2, "green");
            SetHomeBlock(gui.LockHome3, "yellow");
            SetHomeBlock(gui.LockHome4, "blue");
        }
        public async Task RelocateAsync(List<Piece> piece, Piece pieceClone, string playsound = "move")
        {
            string colorKey = char.ToLower(piece[0].Name[0]).ToString();

            List<Piece> allPieces = GetAllPieces();
            // Hide indicators on all tokens.
            foreach (Piece p in allPieces)
                gui.getPieceToken(p).ShowHideIndicator(false);

            // Update the source cell by excluding the moving piece.
            adjustPiceImage(piece[0], allPieces, excludeMoving: true);
            // **Pre-move Phase:**
            // Update the moving token explicitly to use the single image version.
            if (piece.Count == 1)
                gui.getPieceToken(piece[0]).UpdateView(GetDefaultImage(colorKey, ""));
            if (piece.Count == 2)
            {
                gui.getPieceToken(piece[0]).UpdateView(GetDefaultImage(colorKey, "_2"));
                gui.getPieceToken(piece[1]).UpdateView(GetDefaultImage(colorKey, "_2"));
            }

            // Perform the relocation animation.
            await RelocateHelper(piece, pieceClone, playsound);
            // **Post-move Phase:**
            // Now update the board normally, including the moving piece in the grouping.
            adjustPiceImage(piece[0], allPieces, excludeMoving: false);
        }
        private void adjustPiceImage(Piece movingPiece, List<Piece> allPieces, bool excludeMoving)
        {
            // 1. Get the color key from the moving piece.
            string colorKey = char.ToLower(movingPiece.Name[0]).ToString();

            // 2. Filter all pieces that share the same color.
            // When excludeMoving is true, skip the moving piece.
            var sameColorPieces = allPieces
                .Where(p => char.ToLower(p.Name[0]).ToString() == colorKey &&
                           (!excludeMoving || p != movingPiece))
                .ToList();

            // 3. Group those pieces by their board cell.
            var boardGroups = sameColorPieces.GroupBy(p => engine.EngineHelper.getPieceBox(p));

            // 4. Process each board group.
            foreach (var boardGroup in boardGroups)
            {
                // Determine suffix: if more than one piece is on the same cell, use the double image.
                string suffix = boardGroup.Count() > 1 ? "_" + boardGroup.Count() : "";
                string imagePath = GetDefaultImage(colorKey, suffix);

                // Process each group.
                foreach (var p in boardGroup)
                {
                    var token = gui.getPieceToken(p);
                    if (token.ImageContainer != imagePath)
                        token.UpdateView(imagePath);
                }
            }
        }
        public async Task RelocateHelper(List<Piece> pieces, Piece pieceClone, string playsound = "move")
        {
            engine.EngineHelper.animationBlock = true;
            pieceClone = pieces[0].Clone();
            uint animTime = 10;


            if (pieceClone.Location <= pieces[0].Location)
            {
                if (pieceClone.Location != pieces[0].Location)
                    pieceClone.Jump(engine, 1, true);

                string PBC = engine.EngineHelper.getPieceBox(pieceClone);
                double x = engine.EngineHelper.originalPath[PBC][1] * (Alayout.Width / 15);
                double y = engine.EngineHelper.originalPath[PBC][0] * (Alayout.Height / 15);

                await RunAnimationAsync(pieces, x, y, animTime, "Move");

                if (pieceClone.Location != pieces[0].Location)
                    await RelocateHelper(pieces, pieceClone, playsound);
                else
                {
                    engine.EngineHelper.animationBlock = false;
                    await ResizePieces();
                }
            }
            while (engine.EngineHelper.animationBlock)
                await Task.Delay(20);
        }
        public Task RunAnimationAsync(List<Piece> pieces, double targetX, double targetY, uint duration, String AnimationType)
        {
            switch (AnimationType)
            {
                case "Move":
                    var moves = pieces
                        .Select(piece =>
                        {
                            var token = gui.getPieceToken(piece);
                            // TranslateToAsync animates both TranslationX and TranslationY
                            return token.TranslateTo(targetX, targetY, duration, Easing.CubicIn);
                        })
                        .ToArray();

                    // Task.WhenAll will complete when every TranslateToAsync is done.
                    return Task.WhenAll(moves);
                case "Scale":
                    var scaleTasks = pieces.Select(async piece =>
                    {
                        var token = gui.getPieceToken(piece);
                        token.TranslateTo(targetX, targetY, duration, Easing.Linear);
                        token.ScaleTo(1.0, 100);
                    });
                    return Task.WhenAll(scaleTasks);
            }
            // Kick off a TranslateToAsync for each piece and return
            // a Task that completes when all of them are done.
            return Task.CompletedTask;
        }
        private async Task ResizePieces()
        {
            List<Piece> allPieces = GetAllPieces();

            // Group pieces by their board key from getPieceBox.
            var boardGroups = allPieces.GroupBy(piece => engine.EngineHelper.getPieceBox(piece));
            foreach (var boardGroup in boardGroups)
            {
                string boxKey = boardGroup.Key;
                var piecesInBox = boardGroup.ToList();

                // Retrieve the center coordinates from originalPath.
                if (!engine.EngineHelper.originalPath.TryGetValue(boxKey, out int[] boardCoords))
                {
                    continue;
                }
                double centerX = boardCoords[1] * (Alayout.Width / 15.0);
                double centerY = boardCoords[0] * (Alayout.Height / 15.0);

                // Group pieces by player (using first letter of piece.Name, case-insensitive).
                var playerGroups = piecesInBox
                                   .GroupBy(piece => piece.Name.Substring(0, 1).ToLower())
                                   .ToList();
                int numPlayerGroups = playerGroups.Count;

                // If only one player's tokens are in the cell, place them centered.
                if (numPlayerGroups == 1)
                    await RunAnimationAsync(playerGroups[0].ToList(), centerX, centerY, 100, "Scale");
                else
                {
                    // Define the offset distance.
                    double groupSpacing = 5.0; // adjust as needed
                    int index = 0;
                    // Order groups by key for consistency.
                    foreach (var pg in playerGroups.OrderBy(g => g.Key))
                    {
                        double subCenterX = centerX;
                        double subCenterY = centerY;

                        if (numPlayerGroups == 2)
                        {
                            // For 2 players:
                            // Group 0: Top Left; Group 1: Bottom Right.
                            if (index == 0)
                            {
                                subCenterX = centerX - groupSpacing;
                                subCenterY = centerY - groupSpacing + 4;
                            }
                            else if (index == 1)
                            {
                                subCenterX = centerX + groupSpacing;
                                subCenterY = centerY + groupSpacing;
                            }
                        }
                        else if (numPlayerGroups == 3)
                        {
                            // For 3 players:
                            // Group 0: Top Left; Group 1: Top Right; Group 2: Bottom Center.
                            if (index == 0)
                            {
                                subCenterX = centerX - groupSpacing;
                                subCenterY = centerY - groupSpacing + 4;
                            }
                            else if (index == 1)
                            {
                                subCenterX = centerX + groupSpacing;
                                subCenterY = centerY - groupSpacing + 4;
                            }
                            else if (index == 2)
                            {
                                subCenterX = centerX;
                                subCenterY = centerY + groupSpacing;
                            }
                        }
                        else if (numPlayerGroups >= 4)
                        {
                            // For 4 or more players:
                            // Group 0: Top Left; Group 1: Top Right; Group 2: Bottom Left; Group 3: Bottom Right.
                            if (index == 0)
                            {
                                subCenterX = centerX - groupSpacing;
                                subCenterY = centerY - groupSpacing + 4;
                            }
                            else if (index == 1)
                            {
                                subCenterX = centerX + groupSpacing;
                                subCenterY = centerY - groupSpacing + 4;
                            }
                            else if (index == 2)
                            {
                                subCenterX = centerX - groupSpacing;
                                subCenterY = centerY + groupSpacing;
                            }
                            else if (index == 3)
                            {
                                subCenterX = centerX + groupSpacing;
                                subCenterY = centerY + groupSpacing;
                            }
                            else
                            {
                                // For extra groups beyond 4, default to center or add more custom placements.
                                subCenterX = centerX;
                                subCenterY = centerY;
                            }
                        }
                        // Place all tokens for this player's group at the computed sub-center.
                        await RunAnimationAsync(pg.ToList(), subCenterX, subCenterY, 100, "Scale");
                        index++;
                    }
                }
            }

            SetHomeBlock(gui.LockHome1, "red");
            SetHomeBlock(gui.LockHome2, "green");
            SetHomeBlock(gui.LockHome3, "yellow");
            SetHomeBlock(gui.LockHome4, "blue");

            
                string score = $"Score : {engine.EngineHelper.getPlayer(playerColor.ToLower()).Score}";
                if (score != ScoreText.Text)
                    ScoreText.Text = score;
        }
        private string GetDefaultImage(string colorLetter, string suffics)
        {
            switch (colorLetter)
            {
                case "r": return Constants.Skins.RedToken.Replace(".png", suffics + ".png");
                case "g": return Constants.Skins.GreenToken.Replace(".png", suffics + ".png");
                case "y": return Constants.Skins.YellowToken.Replace(".png", suffics + ".png");
                case "b": return Constants.Skins.BlueToken.Replace(".png", suffics + ".png");
                default: return "default.png"; // Fallback in case no matching color is found.
            }
        }
        public List<Piece> GetAllPieces()
        {
            List<Piece> allPieces = new List<Piece>();
            foreach (var player in engine.EngineHelper.players)
                foreach (var piece in player.Pieces)
                    allPieces.Add(piece);
            return allPieces;
        }

        public void StartProgressAnimation(string SeatName)
        {
            List<Piece> allPieces = GetAllPieces();
            foreach (Piece p in allPieces)
                gui.getPieceToken(p).ShowHideIndicator(false);

            List<Piece> moveablePieces = engine.EngineHelper.currentPlayer.Pieces.Where(p => p.Moveable || p.DoubleMoveable).ToList();

            foreach (Piece p in moveablePieces)
                if (engine.EngineHelper.gameState == "MovePiece")
                    gui.getPieceToken(p).ShowHideIndicator(true);

            GetPlayerSeat(SeatName).StartProgressAnimation();
        }

        public void StopDice(string SeatName, int dicevalue)
        {
            var seat = GreenPlayerSeat;
            if (SeatName == "red")
                seat = RedPlayerSeat;
            if (SeatName == "green")
                seat = GreenPlayerSeat;
            if (SeatName == "yellow")
                seat = YellowPlayerSeat;
            if (SeatName == "blue")
                seat = BluePlayerSeat;

            seat.StopDice(dicevalue);
        }

        public void AnimateDice(string SeatName)
        {
            var seat = GreenPlayerSeat;
            switch (SeatName)
            {
                case "red":
                    seat = RedPlayerSeat;
                    break;
                case "green":
                    seat = GreenPlayerSeat;
                    break;
                case "yellow":
                    seat = YellowPlayerSeat;
                    break;
                case "blue":
                    seat = BluePlayerSeat;
                    break;
            }

            seat.AnimateDice();
        }

        public void StopProgressAnimation(string SeatName)
        {
            GetPlayerSeat(SeatName).StopProgressAnimation();
        }
        public void PlayerLeftSeat(string SeatColor, bool SendToServer = true)
        {
            GetPlayerSeat(SeatColor).PlayerLeft();
        }
        public async Task ShowResults(string seats, string GameType, string GameCost)
        {
            await Task.Delay(2000);
            
                // Get seat details for both winners and add them to the list
                List<PlayerDto> playerDtos = new List<PlayerDto>();
                string winner1 = seats.Split(",")[0];
                string winner2 = seats.Split(",")[1];
                //public String seatColor = "";
                //public String PlayerName = "";
                //public String PlayerImageSource = "";
                // Separate winners and losers
                var winners = this.seats.Where(p => p.PlayerColor == winner1 || p.PlayerColor == winner2).ToList();
                var losers = this.seats.Where(p => p.PlayerColor != winner1 && p.PlayerColor != winner2).ToList();

                // Add winners first
                foreach (var winner in winners)
                    if (winner != null)
                        playerDtos.Add(winner);

                // Add losers next
                foreach (var loser in losers)
                    if (loser != null)
                        playerDtos.Add(loser);
                // Pass the list to the UI for displaying results
                
                Console.WriteLine(playerDtos+ GameType+ GameCost);
        }
        public async void PlayerDiceClicked(String SeatColor, String DiceValue, String Piece1, String Piece2, bool SendToServer = true)
        {
            if (engine.EngineHelper.checkTurn(SeatColor, "RollDice"))
            {
                gui.red.reset();
                gui.green.reset();
                gui.yellow.reset();
                gui.blue.reset();

                // Handle the dice click for the green player
                //check turn
                var seat = gui.red;
                if (SeatColor == "red")
                    seat = gui.red;
                if (SeatColor == "green")
                    seat = gui.green;
                if (SeatColor == "yellow")
                    seat = gui.yellow;
                if (SeatColor == "blue")
                    seat = gui.blue;

                seat.AnimateDice();


                String result = await engine.SeatTurn(SeatColor, DiceValue, Piece1, Piece2);
                Console.WriteLine($"1 Local : {result}");
                engine.EngineHelper.index++;
            }

            foreach (var piece in engine.EngineHelper.currentPlayer.Pieces)
            {
                // Safely update the UI
                Alayout.Remove(gui.getPieceToken(piece));
                Alayout.Add(gui.getPieceToken(piece));
            }
            //Engine.PlayGame();
        }
        public async Task MovePiece(String piece1String, String piece2String, bool SendToServer = true)
        {
            String result = "";

            result = await engine.MovePieceAsync(piece1String, piece2String);
            engine.EngineHelper.index++;

            Console.WriteLine(result);
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
           
        }
    }
}