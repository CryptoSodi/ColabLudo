
using CommunityToolkit.Maui.Views;
using LudoClient.Constants;
using LudoClient.ControlView;
using LudoClient.Popups;
using SharedCode;
using SharedCode.Constants;
using SharedCode.CoreEngine;
using SimpleToolkit.Core;
using System.Text.Json;

namespace LudoClient.CoreEngine;

public partial class Game : ContentPage
{//For Controling the function calls from other players and IE DiceRoll and Pice Click in multiplayer
    public string playerColor = "";
    public Engine engine;
    Gui gui;
    public PlayerSeat RedPlayerSeat;
    public PlayerSeat GreenPlayerSeat;
    public PlayerSeat YellowPlayerSeat;
    public PlayerSeat BluePlayerSeat;
    public PlayerSeat GetPlayerSeat(string seatColor)
    {
        if (seatColor == "red" || seatColor == "Red")
            return gui.red;
        else if (seatColor == "green" || seatColor == "Green")
            return gui.green;
        else if (seatColor == "yellow" || seatColor == "Yellow")
            return gui.yellow;
        else
            return gui.blue;
    }
    public Game(string gameType, string seatsOrPlayerCount, string playerColor = "")
    {
        if (playerColor == "")
        {
            seats = JsonSerializer.Deserialize<List<PlayerDto>>(seatsOrPlayerCount);
            var player = seats?.FirstOrDefault(p => p.PlayerId == UserInfo.Instance.Id);
            Build("Online", gameType, player.PlayerColor);
        }
        else
            Build(gameType, seatsOrPlayerCount, playerColor);
    }
    List<PlayerDto>? seats;
    private void updateSeat(PlayerSeat playerSeat)
    {
        try
        {
            var player = seats?.FirstOrDefault(p => p.PlayerColor.ToLower() == playerSeat.seatColor);
            if(player!=null)
            playerSeat.showAuto(player.PlayerName, player.PlayerPicture, false, false);
        }
        catch (Exception) {}
    }
    private void Build(string gameType, string playerCount, string playerColor)
    {
        InitializeComponent();
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

        switch (playerCount)
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

        gui = new Gui(red1, red2, red3, red4, gre1, gre2, gre3, gre4, blu1, blu2, blu3, blu4, yel1, yel2, yel3, yel4, RedPlayerSeat, GreenPlayerSeat, YellowPlayerSeat, BluePlayerSeat);

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

        PlayerSeat playerSeat = playerColor switch
        {
            "Red" => gui.red,
            "Green" => gui.green,
            "Yellow" => gui.yellow,
            "Blue" => gui.blue,
            _ => null
        };

        var colors = new[] { ("Red", gui.red), ("Green", gui.green), ("Yellow", gui.yellow), ("Blue", gui.blue) };
        if (gameType == "Online")
        {
            foreach (var (color, seat) in colors)
                updateSeat(GetPlayerSeat(color));

            //    if (playerColor != color)
            //        seat.hideAuto($" {Array.IndexOf(colors, (color, seat)) + 1}", "player.png", false, false);

            //playerSeat.showAuto(UserInfo.Instance.Name, UserInfo.Instance.PictureUrl, false, false);
            engine = new Engine(gameType, playerCount, "Red");
        }
        else
        {
            foreach (var (color, seat) in colors)
                if (playerColor != color)
                    if (gameType == "Computer")
                        seat.hideAuto($"Computer {Array.IndexOf(colors, (color, seat)) + 1}", "player.png", true, true);
                    else
                        seat.showAuto($"Player {Array.IndexOf(colors, (color, seat)) + 1}", "player.png", false, false);

            playerSeat?.showAuto(UserInfo.Instance.Name, UserInfo.Instance.PictureUrl, false, false);
            engine = new Engine(gameType, playerCount, playerColor);
        }
        
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
        int rotation = engine.EngineHelper.SetRotation(playerColor);
        Glayout.RotateTo(rotation);

        foreach (var player in engine.EngineHelper.players)
            foreach (var piece in player.Pieces)
                Alayout.Add(gui.getPieceToken(piece));

        // Handle layout size changes
        Alayout.SizeChanged += (sender, e) =>
        {
            Console.WriteLine("The layout has been loaded and rendered.");
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
        MusicSwitch.init(".png");
    }
    public async Task ShowResults(string seats)
    {
        ClientGlobalConstants.results.init(JsonSerializer.Deserialize<List<PlayerDto>>(seats));
        ClientGlobalConstants.dashBoard.Navigation.PushAsync(ClientGlobalConstants.results);
        ClientGlobalConstants.FlushOld();
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
                _ = RelocateAsync(engine.EngineHelper.players[i].Pieces[j], engine.EngineHelper.players[i].Pieces[j]);
            }
    }
    public async Task RelocateAsync(Piece piece, Piece pieceClone)
    {
        engine.EngineHelper.animationBlock = true;

        uint animTime = 200;
        if (engine.EngineHelper.stopAnimate)
        {
            animTime = 40;
            pieceClone = piece.Clone();
        }
        //piece.Position
        //player.StartPosition
        string PB = engine.EngineHelper.getPieceBox(piece);

        if (pieceClone.Location < piece.Location)
        {
            pieceClone.Jump(engine, 1, true);
            string PBC = engine.EngineHelper.getPieceBox(pieceClone);

            double x = engine.EngineHelper.originalPath[PBC][1] * (Alayout.Width / 15);
            double y = engine.EngineHelper.originalPath[PBC][0] * (Alayout.Height / 15);

            _ = gui.getPieceToken(piece).TranslateTo(x, y, animTime, Easing.CubicIn).ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    if (pieceClone.Location != piece.Location)
                        _ = RelocateAsync(piece, pieceClone);
                    else
                    {
                        engine.EngineHelper.animationBlock = false;
                        ResizePieces();
                    }   
                }
                else if (t.IsFaulted)
                {
                    Console.WriteLine($"Error during translation: {t.Exception?.Message}");
                }
            });
        }
        else
        {
            double x = engine.EngineHelper.originalPath[PB][1] * (Alayout.Width / 15);
            double y = engine.EngineHelper.originalPath[PB][0] * (Alayout.Height / 15);
            _ = gui.getPieceToken(piece).TranslateTo(x, y, animTime, Easing.CubicIn);
            engine.EngineHelper.animationBlock = false;
            ResizePieces();
        }
        while (engine.EngineHelper.animationBlock)
        {
            await Task.Delay(20);
        }
    }
    private void ResizePieces()
    {
        Console.WriteLine("Perform Resize");
        List<Piece> allPieces = new List<Piece>();
        foreach (var player in engine.EngineHelper.players)
            foreach (var piece in player.Pieces)
                allPieces.Add(piece);

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
            {
                foreach (var piece in playerGroups[0])
                {
                    var token = gui.getPieceToken(piece);
                    _ = token.TranslateTo(centerX, centerY, 100, Easing.Linear);
                    token.ScaleTo(1.0, 100);
                }
            }
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
                            subCenterY = centerY - groupSpacing+4;
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
                    foreach (var piece in pg)
                    {
                        var token = gui.getPieceToken(piece);
                        _ = token.TranslateTo(subCenterX, subCenterY, 100, Easing.Linear);
                        token.ScaleTo(1.0, 100);
                    }
                    index++;
                }
            }
        }
    }
    public void PlayerPieceClicked(String PieceName, bool SendToServer=true)
    {
        //start animation
        // Handle the dice click for the green player
        _ = engine.MovePieceAsync(PieceName, SendToServer);
        //stop animmation
    }
    public void PlayerDiceClicked(String SeatColor, String DiceValue, String Piece, bool SendToServer = true)
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

            engine.SeatTurn(SeatColor, DiceValue, Piece, SendToServer);
        }
        foreach (var piece in engine.EngineHelper.currentPlayer.Pieces)
        {
            // Safely update the UI
            Alayout.Remove(gui.getPieceToken(piece));
            Alayout.Add(gui.getPieceToken(piece));
        }
        //Engine.PlayGame();
    }
    public void StartProgressAnimation(string SeatName)
    {
        GetPlayerSeat(SeatName).StartProgressAnimation();
    }
    public void StopProgressAnimation(string SeatName)
    {
        GetPlayerSeat(SeatName).StopProgressAnimation();
    }
    public void AnimateDice(string SeatName){
        var seat = GreenPlayerSeat;
        if (SeatName == "red")
            seat = RedPlayerSeat;
        if (SeatName == "green")
            seat = GreenPlayerSeat;
        if (SeatName == "yellow")
            seat = YellowPlayerSeat;
        if (SeatName == "blue")
            seat = BluePlayerSeat;

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
    MessageBox mb = null;
    private async void ExitToLobby(object sender, EventArgs e)
    {
        try{PopoverButton.HideAttachedPopover();}catch (Exception){}

        if (mb != null)
            return;
        if (engine.EngineHelper.gameType == "Online")
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
            if (engine.EngineHelper.gameType == "Online")
                GlobalConstants.MatchMaker.LeaveCloseLobby(UserInfo.Instance.Id);
            else
            {
                PopoverButton.HideAttachedPopover();
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
        // Insert your custom logic here
        // For example, display a confirmation dialog (note: async work must be handled carefully since this method is synchronous)
        ExitToLobby(null, null);
        // Prevent back navigation:
        return true;
        // Or to allow it:
        // return base.OnBackButtonPressed();
    }
}