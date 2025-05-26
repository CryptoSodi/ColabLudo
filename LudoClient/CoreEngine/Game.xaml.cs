using CommunityToolkit.Maui.Views;
using LudoClient.Constants;
using LudoClient.ControlView;
using LudoClient.Popups;
using Microsoft.AspNetCore.SignalR.Client;
using SharedCode;
using SharedCode.Constants;
using SharedCode.CoreEngine;
using SimpleToolkit.Core;
using System.Text.Json;

namespace LudoClient.CoreEngine;

public partial class Game : ContentPage
{//For Controling the function calls from other players and IE DiceRoll and Pice Click in multiplayer
    Piece tempPiece = null;
    public string playerColor = "";
    public Engine engine;
    Gui gui;
    public PlayerSeat RedPlayerSeat;
    public PlayerSeat GreenPlayerSeat;
    public PlayerSeat YellowPlayerSeat;
    public PlayerSeat BluePlayerSeat;
    string gameMode;
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
    List<PlayerDto>? seats = new List<PlayerDto>();
    public Game(string gameMode, string gameType, string playerColor = "", string seatsData = "", string rollsString = "")
    {
        this.gameMode = gameMode;
        //new List<PlayerDto>();
        //PlayerDto red = new PlayerDto();
        //red.PlayerColor = "Red";
        //PlayerDto gre = new PlayerDto();
        //gre.PlayerColor = "Green";
        //PlayerDto yel = new PlayerDto();
        //yel.PlayerColor = "Yellow";
        //PlayerDto blu = new PlayerDto();
        //blu.PlayerColor = "Blue";
        //seats.Add(red);
        //seats.Add(gre);
        //seats.Add(yel);
        //seats.Add(blu);
        if (seatsData != "")
        {
            seats = JsonSerializer.Deserialize<List<PlayerDto>>(seatsData);
            var player = seats?.FirstOrDefault(p => p.PlayerId == UserInfo.Instance.Id);
            this.playerColor = player.PlayerColor;
            Build("Client", gameType, seats.Count + "", player.PlayerColor, rollsString);
        }
        else
        {
            this.playerColor = playerColor;
            Build(gameMode, gameType, gameType == "22" ? "4" : gameType, playerColor, rollsString);
        }
    }
    private async Task Build(string gameMode, string gameType, string playerCount, string playerColor, string rollsString = "")
    {
        InitializeComponent();
        /* CHAT MANAGEMENT*/
        
        GlobalConstants.MatchMaker.ReceiveChatMessage += UpdateMessages;        
        
        GlobalConstants.MatchMaker.UserConnectedSetID();

        ChatMessages cm = new();
        cm.SenderId = UserInfo.Instance.Id;
        cm.SenderName = UserInfo.Instance.Name;
        cm.SenderPicture = UserInfo.Instance.PictureUrl;
        cm.ReceiverId = -1;
        cm.ReceiverName = "";
        cm.Message = "";
        cm.Time = DateTime.Now;

        GlobalConstants.MatchMaker?.SendChatMessageAsync(cm, GlobalConstants.RoomCode).ContinueWith(t =>
        {
            if (t.Status == TaskStatus.RanToCompletion)
            {
                List<ChatMessages> messages = t.Result;
                UpdateMessages(this, (messages));
            }
        });
        ChatScrollView.IsVisible = false;
        ChatScrollView.InputTransparent = true;
        ChatScrollView.IsEnabled = false;
        if (gameMode != "Client")
        {
            MessageEntryContainer.IsVisible = false;
            MessageEntryContainer.IsEnabled = false;
        }
            /* END CHAT MANAGEMENT*/
            // Create RedPlayerSeat
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
        // Ensure the player's color is always in Row2
        Row1.Children.Clear();
        Row2.Children.Clear();

        if (gameMode == "Client")
        {
            //Inject Chat panel

            // Get the received colors from the server
            List<string> availableSeats = seats.Select(s => s.PlayerColor).ToList(); // Extract received colors
            // Place player at bottom (Row2)
            Row2.Children.Add(GetPlayerSeat(playerColor));
            availableSeats.Remove(playerColor); // Remove player from the list
            switch (availableSeats.Count)
            {
                // Now distribute remaining seats based on count
                case 1: // 2-player game
                    Row1.Children.Add(GetPlayerSeat(availableSeats[0])); // Place the other player in Row2
                    break;
                case 2:
                    string firstOpponent = availableSeats[0];  // First color in received order
                    string secondOpponent = availableSeats[1]; // Second color in received order

                    if (playerColor == "Yellow")
                    {
                        // Yellow should have Green in Row2 and Red in Row1
                        Row2.Children.Add(GetPlayerSeat(secondOpponent)); // Green at bottom
                        Row1.Children.Add(GetPlayerSeat(firstOpponent));  // Red at top
                    }
                    else if (playerColor == "Red")
                    {
                        // Red should be alone in Row2, Green & Yellow in Row1
                        Row1.Children.Add(GetPlayerSeat(firstOpponent));  // Green at top
                        Row1.Children.Add(GetPlayerSeat(secondOpponent)); // Yellow at top
                    }
                    else
                    {
                        // Default case: playerColor is Green
                        Row2.Children.Add(GetPlayerSeat(firstOpponent)); // Red at bottom
                        Row1.Children.Add(GetPlayerSeat(secondOpponent)); // Yellow at top
                    }
                    break;
                case 3:
                    string bottomOpponent = "";
                    List<string> topOpponents = new List<string>();

                    if (playerColor == "Red")
                    {
                        bottomOpponent = "Blue";
                        topOpponents.Add("Green");
                        topOpponents.Add("Yellow");
                    }
                    else if (playerColor == "Blue")
                    {
                        bottomOpponent = "Yellow";
                        topOpponents.Add("Red");
                        topOpponents.Add("Green");
                    }
                    else if (playerColor == "Yellow")
                    {
                        bottomOpponent = "Green";
                        topOpponents.Add("Blue");
                        topOpponents.Add("Red");
                    }
                    else if (playerColor == "Green")
                    {
                        bottomOpponent = "Red";
                        topOpponents.Add("Yellow");
                        topOpponents.Add("Blue");
                    }

                    // Place the bottom row: player's seat was already added; now add the bottom opponent.
                    Row2.Children.Add(GetPlayerSeat(bottomOpponent));
                    // Place the top row with the two opponents in the desired order.
                    Row1.Children.Add(GetPlayerSeat(topOpponents[0]));
                    Row1.Children.Add(GetPlayerSeat(topOpponents[1]));
                    break;
            }
        }
        else
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
        if (gameMode == "Client")
        {
            foreach (var (color, seat) in colors)
                try
                {
                    var playerSeat = GetPlayerSeat(color);
                    PlayerDto player = seats?.FirstOrDefault(p => p.PlayerColor.ToLower() == playerSeat.seatColor);
                    if (player != null)
                        playerSeat.showAuto(player.PlayerName, player.PlayerPicture, false, false);
                }
                catch (Exception) { }
            //    if (playerColor != color)
            //        seat.hideAuto($" {Array.IndexOf(colors, (color, seat)) + 1}", "player.png", false, false);

            //playerSeat.showAuto(UserInfo.Instance.Name, UserInfo.Instance.PictureUrl, false, false);            
            playerColor = "Red";
        }
        else
        {
            foreach (var (color, seat) in colors)
                if (playerColor != color)
                    if (gameMode == "Computer")
                        seat.hideAuto($"Computer {Array.IndexOf(colors, (color, seat)) + 1}", "player.png", true, true);
                    else
                        seat.showAuto($"Player {Array.IndexOf(colors, (color, seat)) + 1}", "player.png", false, false);

            GetPlayerSeat(playerColor)?.showAuto(UserInfo.Instance.Name, UserInfo.Instance.PictureUrl, false, false);
        }
        engine = new Engine(gameMode, gameType, playerCount, playerColor, rollsString);

        gui.red.EngineHelper = engine.EngineHelper;
        gui.green.EngineHelper = engine.EngineHelper;
        gui.yellow.EngineHelper = engine.EngineHelper;
        gui.blue.EngineHelper = engine.EngineHelper;
        if (!engine.EngineHelper.stopAnimate)
            StartProgressAnimation(engine.EngineHelper.currentPlayer.Color);

        engine.StopDice += new Engine.CallbackEventHandler(StopDice);
        engine.AnimateDice += new Engine.Callback_AnimateDice_EventHandler(AnimateDice);
        engine.StartProgressAnimation += new Engine.CallbackEventHandlerStartProgressAnimation(StartProgressAnimation);
        engine.StopProgressAnimation += new Engine.CallbackEventHandlerStopProgressAnimation(StopProgressAnimation);
        engine.RelocateAsync += new Engine.CallbackEventHandlerRelocateAsync(RelocateAsync);
        engine.ShowResults += new Engine.CallbackEventHandlerShowResults(ShowResults);
        engine.PlayerLeftSeat += new Engine.CallbackEventHandlerPlayerLeft(PlayerLeftSeat);
        // Set rotation based on player color
        int rotation = engine.EngineHelper.SetRotation(this.playerColor);
        Glayout?.RotateTo(rotation);

        foreach (var player in engine.EngineHelper.players)
            foreach (var piece in player.Pieces)
                Alayout.Add(gui.getPieceToken(piece));

        SetHomeBlock(gui.LockHome1, "red");
        SetHomeBlock(gui.LockHome2, "green");
        SetHomeBlock(gui.LockHome3, "yellow");
        SetHomeBlock(gui.LockHome4, "blue");
        // Handle layout size changes
        Alayout.SizeChanged += (sender, e) =>
        {
            Pupulate(rotation);
        };

        // Pupulate(rotation);
        foreach (var seat in new[] { gui.red, gui.green, gui.yellow, gui.blue })
            seat.TimerTimeout += engine.TimerTimeoutAsync;

        //Event Handelers
        GreenPlayerSeat.OnDiceClicked += PlayerDiceClicked;
        YellowPlayerSeat.OnDiceClicked += PlayerDiceClicked;
        RedPlayerSeat.OnDiceClicked += PlayerDiceClicked;
        BluePlayerSeat.OnDiceClicked += PlayerDiceClicked;

        red1.OnPieceClicked += PlayerPieceClicked;
        red2.OnPieceClicked += PlayerPieceClicked;
        red3.OnPieceClicked += PlayerPieceClicked;
        red4.OnPieceClicked += PlayerPieceClicked;
        gre1.OnPieceClicked += PlayerPieceClicked;
        gre2.OnPieceClicked += PlayerPieceClicked;
        gre3.OnPieceClicked += PlayerPieceClicked;
        gre4.OnPieceClicked += PlayerPieceClicked;
        yel1.OnPieceClicked += PlayerPieceClicked;
        yel2.OnPieceClicked += PlayerPieceClicked;
        yel3.OnPieceClicked += PlayerPieceClicked;
        yel4.OnPieceClicked += PlayerPieceClicked;
        blu1.OnPieceClicked += PlayerPieceClicked;
        blu2.OnPieceClicked += PlayerPieceClicked;
        blu3.OnPieceClicked += PlayerPieceClicked;
        blu4.OnPieceClicked += PlayerPieceClicked;

        RedPlayerSeat.reset();
        GreenPlayerSeat.reset();
        YellowPlayerSeat.reset();
        BluePlayerSeat.reset();
        SoundSwitch.init(".png");
        VibrationSwitch.init(".png");
        //The Display to show selection of single or double token move
        TokenSelector.IsVisible = true;
        Alayout.Remove(TokenSelector);
        Alayout.Add(TokenSelector);

        double x = engine.EngineHelper.originalPath["p0"][1] * (Alayout.Width / 15) - (TokenSelector.Width / 2) + 10;
        double y = engine.EngineHelper.originalPath["p0"][0] * (Alayout.Height / 15) - TokenSelector.Height - 2;

        TokenSelector?.RotateTo(-rotation);
        await TokenSelector.TranslateTo(x, y, 1, Easing.CubicIn);

        TokenSelector1.UpdateView(GetDefaultImage("r", ""));
        TokenSelector2.UpdateView(GetDefaultImage("r", "_2"));

        if (gameMode != "Client")//If local game init the seats so that results can be built later on
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

        TokenSelector.IsVisible = false;
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
    public async Task ShowResults(string seats, string GameType, string GameCost)
    {
        await Task.Delay(2000);
        if (gameMode == "Client")
        {
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Task.Delay(100);
                ClientGlobalConstants.results = new Results();
                ClientGlobalConstants.results.init(JsonSerializer.Deserialize<List<PlayerDto>>(seats), GameType, GameCost);
            });
            ClientGlobalConstants.dashBoard.Navigation.PushAsync(ClientGlobalConstants.results);
            ClientGlobalConstants.FlushOld();
        }
        else
        {
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
            ClientGlobalConstants.results = new Results();
            ClientGlobalConstants.results.init(playerDtos, GameType, GameCost);
            ClientGlobalConstants.dashBoard.Navigation.PushAsync(ClientGlobalConstants.results);
        }
        // this.ShowPopup(ClientGlobalConstants.results);
    }
    public void PlayerLeftSeat(string SeatColor, bool SendToServer = true)
    {
        GetPlayerSeat(SeatColor).PlayerLeft();
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
                _ = RelocateAsync(pieces, engine.EngineHelper.players[i].Pieces[j]);
            }

        SetHomeBlock(gui.LockHome1, "red");
        SetHomeBlock(gui.LockHome2, "green");
        SetHomeBlock(gui.LockHome3, "yellow");
        SetHomeBlock(gui.LockHome4, "blue");
    }
    public async Task RelocateHelper(List<Piece> pieces, Piece pieceClone)
    {
        engine.EngineHelper.animationBlock = true;

        uint animTime = 200;
        if (engine.EngineHelper.stopAnimate)
        {
            animTime = 40;
            pieceClone = pieces[0].Clone();
        }

        if (pieceClone.Location <= pieces[0].Location)
        {
            if (pieceClone.Location != pieces[0].Location)
                pieceClone.Jump(engine, 1, true);

            string PBC = engine.EngineHelper.getPieceBox(pieceClone);
            double x = engine.EngineHelper.originalPath[PBC][1] * (Alayout.Width / 15);
            double y = engine.EngineHelper.originalPath[PBC][0] * (Alayout.Height / 15);

            await RunAnimationAsync(pieces, x, y, animTime, "Move");

            if (pieceClone.Location != pieces[0].Location)
                await RelocateHelper(pieces, pieceClone);
            else
            {
                engine.EngineHelper.animationBlock = false;
                ResizePieces();
            }
        }

        while (engine.EngineHelper.animationBlock)
            await Task.Delay(20);
    }
    public async Task RelocateAsync(List<Piece> piece, Piece pieceClone)
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
        await RelocateHelper(piece, pieceClone);
        // **Post-move Phase:**
        // Now update the board normally, including the moving piece in the grouping.
        adjustPiceImage(piece[0], allPieces, excludeMoving: false);
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
    }
    // Helper method to return the default image file name for a given color.
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
    public async void PlayerPieceClicked(String piece1String, String piece2String, bool SendToServer = true)
    {
        TokenSelector.IsVisible = false;
        if (!engine.EngineHelper.checkTurn(piece1String, "MovePiece"))
            return;

        if (!SendToServer || piece2String != "") {
            tempPiece = null;
            MovePiece(piece1String, piece2String, SendToServer);
            return;
        }
        try
        {
            Piece piece1 = engine.EngineHelper.GetPiece(engine.EngineHelper.currentPlayer.Pieces, piece1String);
            string currentBox = "";
            int ownAtBox = 0;
            
            if (engine.EngineHelper.currentPlayer.Color.ToLower().Contains(piece1String.Replace("1", "").Replace("2", "").Replace("3", "").Replace("4", "")) && (engine.EngineHelper.diceValue == 2 || engine.EngineHelper.diceValue == 4 || engine.EngineHelper.diceValue == 6))
            {
                if (piece1 != null)
                {
                    currentBox = engine.EngineHelper.getPieceBox(piece1);
                    ownAtBox = engine.board?[currentBox].Count(x => x.Color == piece1.Color) ?? 0;
                }
            }

            if (ownAtBox > 1 && piece1?.Location <= 51)
            {
                //TODO
                //This code sets the location of TokenSelector

                Piece piece2 = engine.board?[currentBox].Where(p => p != piece1 && p.Color == piece1.Color).First();
                if(!piece1.Moveable && piece1.DoubleMoveable)
                {
                    tempPiece = null;
                    await MovePiece(piece1.Name, piece2.Name, SendToServer);
                    return;
                }

                string colorKey = char.ToLower(piece1.Name[0]).ToString();
                TokenSelector1.piece = GetDefaultImage(colorKey, "");
                TokenSelector2.piece = GetDefaultImage(colorKey, "_2");

                tempPiece = piece1;
                Token token = gui.getPieceToken(piece1);

                double offsetX = (token.Width / 2);
                double offsetY = 1;

                if (currentBox == "p10" || currentBox == "p11" || currentBox == "p12")
                    offsetX = offsetX + (80 / 2) - 6;
                if (currentBox == "p22" || currentBox == "p23" || currentBox == "p24" || currentBox == "p25" || currentBox == "p26") // DONE
                    offsetY = offsetY - 50 - token.Height - 2;
                if (currentBox == "p36" || currentBox == "p37" || currentBox == "p38")
                    offsetX = 6 + offsetX - (80 / 2);

                double x = engine.EngineHelper.originalPath[currentBox][1] * (Alayout.Width / 15) - (80 / 2) + offsetX;
                double y = engine.EngineHelper.originalPath[currentBox][0] * (Alayout.Height / 15) - 50 - offsetY;
                await TokenSelector.TranslateTo(x, y, 1, Easing.CubicIn);
                TokenSelector.IsVisible = true;
            }
            else
            {
                tempPiece = null;
                await MovePiece(piece1.Name, "", SendToServer);
            }
        }
        catch (Exception)
        { }
        //stop animmation
    }
    private async Task MovePiece(String piece1String, String piece2String, bool SendToServer = true)
    {
        string result = await engine.MovePieceAsync(piece1String, piece2String);
        ClientGlobalConstants.game.engine.EngineHelper.index++;
        if (engine.EngineHelper.gameMode == "Client" && SendToServer)
        {
            List<string> results = result.Split(",").ToList();
            GameCommand command = new GameCommand
            {
                SendToClientFunctionName = "MovePiece",
                seatName = "",
                diceValue = "",
                piece1 = results[0],
                piece2 = results[1],
                Index = engine.EngineHelper.index,
                IndexServer = 0
            };

            GlobalConstants.MatchMaker?.SendMessageAsync(command, "MovePiece").ContinueWith(t =>
            {
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    GameCommand resultCommand = t.Result;
                    if (command.Index != resultCommand.Index)
                    {
                        Console.WriteLine("ERROR SERVER OUT OF SYNC AT PIECE");
                    }
                }
                else
                {
                    //ServerpieceName = "Error"; // Handle failure
                }
            });
        }
        Console.WriteLine(result);
    }
    private async void TokenSelected_Clicked(object sender, EventArgs e)
    {
        TokenSelector.IsVisible = false;
        if (sender is ImageButton button)
        {
            var parameter = button.CommandParameter as string;
            // Use the parameter value as needed
            Console.WriteLine($"CommandParameter: {parameter}");
            if (parameter == "2")
            {
                string currentBox = engine.EngineHelper.getPieceBox(tempPiece);
                List<Piece> Piece2 = engine.board?[currentBox].Where(x => x.Color == tempPiece.Color).ToList().Where(x => x.Name != tempPiece.Name).ToList();

                PlayerPieceClicked(tempPiece.Name , Piece2?[0].Name, true);
            }
            else
            {
                if (!engine.EngineHelper.checkTurn(tempPiece.Name, "MovePiece"))
                    return;
                await MovePiece(tempPiece.Name, "");
            }
        }
    }
    public async void PlayerDiceClicked(String SeatColor, String DiceValue, String Piece1, String Piece2, bool SendToServer = true)
    {
        TokenSelector.IsVisible = false;

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
            Console.WriteLine($"Local : {result}");
            ClientGlobalConstants.game.engine.EngineHelper.index++;
            if (engine.EngineHelper.gameMode == "Client" && SendToServer)
            {
                List<string> results = result.Split(",").ToList();
                GameCommand command = new GameCommand
                {
                    SendToClientFunctionName = "DiceRoll",
                    seatName = SeatColor,
                    diceValue = results[0],
                    piece1 = results[1],
                    piece2 = results[2],
                    Index = engine.EngineHelper.index,
                    IndexServer = 0
                };
                GlobalConstants.MatchMaker?.SendMessageAsync(command, "DiceRoll").ContinueWith(t =>
                {
                    if (t.Status == TaskStatus.RanToCompletion)
                    {
                        GameCommand resultCommand = t.Result;
                        if (command.Index != resultCommand.Index)
                        {
                            Console.WriteLine("ERROR SERVER OUT OF SYNC AT DICEROLL");
                        }
                    }
                    else
                    {
                        //ServerpieceName = "Error"; // Handle failure
                    }
                });
            }
        }

        foreach (var piece in engine.EngineHelper.currentPlayer.Pieces)
        {
            // Safely update the UI
            Alayout.Remove(gui.getPieceToken(piece));
            Alayout.Add(gui.getPieceToken(piece));
        }
        Alayout.Remove(TokenSelector);
        Alayout.Add(TokenSelector);
        //Engine.PlayGame();
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
    public void StopProgressAnimation(string SeatName)
    {
        GetPlayerSeat(SeatName).StopProgressAnimation();
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

        if (dicevalue == 0)
        {
            seat.StopDice(6);
            return;
        }
        seat.StopDice(dicevalue);
    }
    private void PopOverClicked(object sender, EventArgs e)
    {
        PopoverButton.ShowAttachedPopover();
    }
    private void QuestionClicked(object sender, EventArgs e)
    {
        PopoverButton.ShowAttachedPopover();
    }
    private void CloseTokenSelector(object sender, EventArgs e)
    {
        // TokenSelector.TranslateTo(0, 0, 10, Easing.CubicIn);
        TokenSelector.IsVisible = false;
    }
    MessageBox mb = null;
    private async void ExitToLobby(object sender, EventArgs e)
    {
        try { PopoverButton.HideAttachedPopover(); } catch (Exception) { }

        if (mb != null)
            return;
        if (engine.EngineHelper.gameMode == "Client")
            if (GlobalConstants.GameCost == 0)
                mb = new MessageBox("Exit", "Are you sure you want to exit?", "Your ranking will be affected!");
            else
                mb = new MessageBox("Exit", "Are you sure you want to exit?", "You will lose your bet amount!");
        else
            mb = new MessageBox("Exit", "Are you sure you want to exit?", "");

        String result = await this.ShowPopupAsync(mb) + "";
        mb = null;
        if (result == "Approve")
        {
            if (engine.EngineHelper.gameMode == "Client")
                GlobalConstants.MatchMaker.LeaveCloseLobby(UserInfo.Instance.Id);
            else
            {
                try
                {
                    PopoverButton.HideAttachedPopover();
                }
                catch (Exception)
                {
                }
                //show pop up for Exit to lobby
                // messageBoxCcnfirm.IsVisible = !messageBoxCcnfirm.IsVisible;
                // GameRecorder.SaveGameHistory();
                engine.cleanGame();
                ClientGlobalConstants.dashBoard.Navigation.PopAsync();
            }
        }
    }
    protected override bool OnBackButtonPressed()
    {
        if (ChatScrollView.IsVisible)
        {
            ChatScrollView.IsVisible = false;
            ChatScrollView.InputTransparent = true;
            ChatScrollView.IsEnabled = false;
            HideKeyboard();
        }
        else
        {
            // Insert your custom logic here
            // For example, display a confirmation dialog (note: async work must be handled carefully since this method is synchronous)
            ExitToLobby(null, null);
        }
        // Prevent back navigation:
        return true;
        // Or to allow it:
        // return base.OnBackButtonPressed();
    }

    //CHAT ENGINE
    private void MessageEntry_Completed(object sender, EventArgs e)
    {
        // … your send logic …
        // Dismiss keyboard:
        OnSendButton_Tapped(null, null);
    }
    private void ShowChat_Tapped(object sender, TappedEventArgs e)
    {
        ChatScrollView.IsVisible = true;
        ChatScrollView.InputTransparent = false;
        ChatScrollView.IsEnabled = true;
        MessageEntry.Focus();
    }
    private void HideChat_Tapped(object sender, TappedEventArgs e)
    {
        ChatScrollView.IsVisible = false;
        ChatScrollView.InputTransparent = true;
        ChatScrollView.IsEnabled = false;
        HideKeyboard();
    }
    private void OnSendButton_Tapped(object sender, TappedEventArgs e)
    {
        HideKeyboard();
        ChatScrollView.IsVisible = true;
        if (MessageEntry.Text != "")
        {
            ChatMessages cm = new();
            cm.SenderId = UserInfo.Instance.Id;
            cm.SenderName = UserInfo.Instance.Name;
            cm.SenderPicture = UserInfo.Instance.PictureUrl;
            //cm.ReceiverId = playerCard.playerID;
            //cm.ReceiverName = playerCard.playerName;
            //cm.ReceiverPicture = playerCard.playerPicture;
            cm.Message = MessageEntry.Text;
            cm.Time = DateTime.Now;
            MessageEntry.Text = "";

            GlobalConstants.MatchMaker?.SendChatMessageAsync(cm, GlobalConstants.RoomCode).ContinueWith(t =>
            {
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    List<ChatMessages> messages = t.Result;
                    UpdateMessages(this, (messages));
                }
            });
        }
    }

    public void UpdateMessages(object sender, List<ChatMessages> messages)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if(messages!=null)
            foreach (ChatMessages cm in messages)
            {
                ChatCard cc = new();
                MessagesListStack.Children.Add(cc);

                if (UserInfo.Instance.Id == cm.SenderId)
                    cc.SetDetails(cm, "Right", cm.SenderColor);
                else
                    cc.SetDetails(cm, "Left", cm.SenderColor);
                // Optional: scroll to bottom

                // After adding your chat cards inside MainThread.BeginInvokeOnMainThread:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(100);
                    await ChatScrollView.ScrollToAsync(0, 40000, true);
                });
            }
        });
    }
    public void HideKeyboard()
    {
        MessageEntry.Unfocus();
#if ANDROID
    var activity = Platform.CurrentActivity;
    var inputMethodManager = activity.GetSystemService(Android.Content.Context.InputMethodService)
                            as Android.Views.InputMethods.InputMethodManager;

    var view = activity.CurrentFocus ?? activity.Window.DecorView;
    inputMethodManager?.HideSoftInputFromWindow(view.WindowToken, Android.Views.InputMethods.HideSoftInputFlags.None);
#endif
    }
}