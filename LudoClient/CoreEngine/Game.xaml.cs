using LudoClient.Constants;
using LudoClient.ControlView;
using LudoClient.Popups;
using LudoClient.Services;
using SharedCode;
using SharedCode.Constants;
using SharedCode.CoreEngine;
using SimpleToolkit.Core;
using System.Text.Json;
namespace LudoClient.CoreEngine;
public partial class Game : ContentPage
{
    //For Controling the function calls from other players and IE DiceRoll and Pice Click in multiplayer
    public bool isInputLocked { get; set; } = false;
    Piece tempPiece = null;
    private double _unitX;
    private double _unitY;
    public string playerColor = "";
    public Engine engine;
    Gui gui;
    string gameMode;
    public PlayerSeat RedPlayerSeat;
    public PlayerSeat GreenPlayerSeat;
    public PlayerSeat YellowPlayerSeat;
    public PlayerSeat BluePlayerSeat;
    List<PlayerDto>? seats = new List<PlayerDto>();
    // A simple persistent store for commands.        
    public readonly List<GameCommand> _commandStore = new List<GameCommand>();
    private readonly HashSet<int> _pendingDiceSend = new();
    private readonly object _pendingDiceSendLock = new();
    private readonly IGamepadInputService _input;
    private readonly GameRtcHelper _rtcHelper;
    private int _boardRotation;
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
    public Game()
    {
        InitializeComponent();
        _rtcHelper = new GameRtcHelper(Alayout, RtcOverlay);
    }
    public void Init(string gameMode, string gameType, string playerColor = "", string seatsData = "", string rollsString = "")
    {
        try
        {
            var sp = Application.Current?.Handler?.MauiContext?.Services ?? throw new InvalidOperationException("No MAUI context yet");
            var input = sp.GetRequiredService<IGamepadInputService>();
        }
        catch (Exception)
        {
        }
        this.gameMode = gameMode;
        if (seatsData != "")
        {
            seats = JsonSerializer.Deserialize<List<PlayerDto>>(seatsData);
            var player = seats?.FirstOrDefault(p => p.PlayerId == UserInfo.Instance.player.PlayerId);
            this.playerColor = player.PlayerColor;
            Build("Client", gameType, seats.Count + "", player.PlayerColor, rollsString);
        }
        else
        {
            this.playerColor = playerColor;
            Build(gameMode, gameType, gameType == "22" ? "4" : gameType, playerColor, rollsString);
        }
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            if (_input != null)
            {
                _input.ButtonChanged += OnButtonChanged;
                _input.AxisChanged += OnAxisChanged;
            }
        }
        catch (Exception)
        {
        }
        _rtcHelper.OnAppearing(GlobalConstants.RoomCode, playerColor);
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _ = _rtcHelper.OnDisappearingAsync();
        if (_input != null)
        {
            _input.ButtonChanged -= OnButtonChanged;
            _input.AxisChanged -= OnAxisChanged;
        }
        GlobalConstants.MatchMaker.ReceiveChatMessage -= UpdateMessages;
    }
    void OnButtonChanged(string device, string button, bool isDown)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Console.WriteLine(button);
            //// EXAMPLES — replace with your game logic:
            //if (button == "A" && isDown) StartGame();
            //if (button == "B" && isDown) Cancel();
            //if (button == "DpadLeft" && isDown) MoveCursor(-1, 0);
            //if (button == "DpadRight" && isDown) MoveCursor(+1, 0);
        });
    }
    void OnAxisChanged(string device, string axis, float value)
    {
        // value typically in [-1 .. +1]
        if (Math.Abs(value) < 0.01f) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Console.WriteLine(axis);
            //if (axis == "LeftStickX") MovePlayer(value, 0);
            //if (axis == "LeftStickY") MovePlayer(0, -value); // invert if you prefer
        });
    }
    private async Task Build(string gameMode, string gameType, string playerCount, string playerColor, string rollsString = "")
    {
        /* END CHAT MANAGEMENT*/
        // Ensure the player's color is always in Row2
        switch (gameType)
            {
                case "2":
                    switch (playerColor)
                    {
                        case "Red":
                            YellowPlayerSeat = s3.setColor("yellow");
                            RedPlayerSeat = s1.setColor("red");
                            GreenPlayerSeat = s2.setColor("green");
                            BluePlayerSeat = s4.setColor("blue");

                            GreenPlayerSeat.IsVisible = BluePlayerSeat.IsVisible = false;
                            break;
                        case "Yellow":
                            YellowPlayerSeat = s1.setColor("yellow");
                            RedPlayerSeat = s3.setColor("red");
                            GreenPlayerSeat = s2.setColor("green");
                            BluePlayerSeat = s4.setColor("blue");
                            GreenPlayerSeat.IsVisible = BluePlayerSeat.IsVisible = false;
                            break;
                        case "Green":
                            GreenPlayerSeat = s1.setColor("Green");
                            BluePlayerSeat = s3.setColor("blue");
                            YellowPlayerSeat = s2.setColor("yellow");
                            RedPlayerSeat = s4.setColor("red");
                            YellowPlayerSeat.IsVisible = RedPlayerSeat.IsVisible = false;
                            break;
                        case "Blue":
                            BluePlayerSeat = s1.setColor("blue");
                            GreenPlayerSeat = s3.setColor("Green");
                            YellowPlayerSeat = s2.setColor("yellow");
                            RedPlayerSeat = s4.setColor("red");
                            YellowPlayerSeat.IsVisible = RedPlayerSeat.IsVisible = false;
                            break;
                    }
                    break;
                case "3":
                    switch (playerColor)
                    {
                        case "Red":
                            RedPlayerSeat = s1.setColor("red");
                            GreenPlayerSeat = s3.setColor("green");
                            YellowPlayerSeat = s4.setColor("yellow");
                            BluePlayerSeat = s2.setColor("blue");
                            BluePlayerSeat.IsVisible = false;
                            break;
                        case "Yellow":
                            RedPlayerSeat = s4.setColor("red");
                            GreenPlayerSeat = s2.setColor("green");
                            YellowPlayerSeat = s1.setColor("yellow");
                            BluePlayerSeat = s3.setColor("blue");
                            GreenPlayerSeat.IsVisible = false;
                            break;
                        case "Green":
                            RedPlayerSeat = s2.setColor("red");
                            GreenPlayerSeat = s1.setColor("green");
                            YellowPlayerSeat = s3.setColor("yellow");
                            BluePlayerSeat = s4.setColor("blue");
                            RedPlayerSeat.IsVisible = false;
                            break;
                        case "Blue":
                            RedPlayerSeat = s3.setColor("red");
                            GreenPlayerSeat = s4.setColor("green");
                            YellowPlayerSeat = s2.setColor("yellow");
                            BluePlayerSeat = s1.setColor("blue");
                            YellowPlayerSeat.IsVisible = false;
                            break;
                    }
                    break;
                case "4":
                case "22":
                    switch (playerColor)
                    {
                        case "Red":
                            RedPlayerSeat = s1.setColor("red");
                            GreenPlayerSeat = s3.setColor("green");
                            YellowPlayerSeat = s4.setColor("yellow");
                            BluePlayerSeat = s2.setColor("blue");
                            break;
                        case "Green":
                            RedPlayerSeat = s2.setColor("red");
                            GreenPlayerSeat = s1.setColor("green");
                            YellowPlayerSeat = s3.setColor("yellow");
                            BluePlayerSeat = s4.setColor("blue");
                            break;
                        case "Yellow":
                            RedPlayerSeat = s4.setColor("red");
                            GreenPlayerSeat = s2.setColor("green");
                            YellowPlayerSeat = s1.setColor("yellow");
                            BluePlayerSeat = s3.setColor("blue");
                            break;
                        case "Blue":
                            RedPlayerSeat = s3.setColor("red");
                            GreenPlayerSeat = s4.setColor("green");
                            YellowPlayerSeat = s2.setColor("yellow");
                            BluePlayerSeat = s1.setColor("blue");
                            break;
                    }
                    break;
            }
        gui = new Gui(red1, red2, red3, red4, gre1, gre2, gre3, gre4, blu1, blu2, blu3, blu4, yel1, yel2, yel3, yel4, LockHome1, LockHome2, LockHome3, LockHome4, RedPlayerSeat, GreenPlayerSeat, YellowPlayerSeat, BluePlayerSeat);
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
                        if (playerColor == color)
                            playerSeat.initAuto(player.PlayerName, player.PlayerPicture, "ShowAuto", false);
                        else
                            playerSeat.initAuto(player.PlayerName, player.PlayerPicture, "HideAuto", false, true);
                }
                catch (Exception) { }
            //    if (playerColor != color)
            //        seat.hideAuto($" {Array.IndexOf(colors, (color, seat)) + 1}", "player.webp", false, false);
            //playerSeat.showAuto(UserInfo.Instance.Name, UserInfo.Instance.PictureUrl, false, false);            
            playerColor = "Red";//This makes sure that the first player on the engine is red to match the same state as on the server
        }
        else
        {
            foreach (var (color, seat) in colors)
                if (playerColor != color)
                    if (gameMode == "Computer")
                        seat.initAuto($"Computer {Array.IndexOf(colors, (color, seat)) + 1}", "player.webp", "HideAll", true);
                    else
                        seat.initAuto($"Player {Array.IndexOf(colors, (color, seat)) + 1}", "player.webp", "ShowAuto", false);

            GetPlayerSeat(playerColor)?.initAuto(UserInfo.Instance.player.Name, UserInfo.Instance.player.PictureUrl, "ShowAuto", false);
        }
        engine = new Engine(gameMode, gameType, playerCount, playerColor, rollsString);

        gui.red.engineHelper = engine.EngineHelper;
        gui.green.engineHelper = engine.EngineHelper;
        gui.yellow.engineHelper = engine.EngineHelper;
        gui.blue.engineHelper = engine.EngineHelper;

        Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(1), () =>
        {
            StartProgressAnimation(engine.EngineHelper.currentPlayer.Color);
        });

        engine.StopDice += new Engine.CallbackEventHandler(StopDice);
        engine.AnimateDice += new Engine.Callback_AnimateDice_EventHandler(AnimateDice);
        engine.StartProgressAnimation += new Engine.CallbackEventHandlerStartProgressAnimation(StartProgressAnimation);
        engine.StopProgressAnimation += new Engine.CallbackEventHandlerStopProgressAnimation(StopProgressAnimation);
        engine.RelocateAsync += new Engine.CallbackEventHandlerRelocateAsync(RelocateAsync);
        engine.ShowResults += new Engine.CallbackEventHandlerShowResults(ShowResults);
        engine.PlayerLeftSeat += new Engine.CallbackEventHandlerPlayerLeft(PlayerLeftSeat);
        // Set rotation based on player color
        int rotation = engine.EngineHelper.SetRotation(this.playerColor);
        _boardRotation = rotation;
        Glayout?.RotateTo(rotation);
        SyncRtcOverlayState();

        foreach (var player in engine.EngineHelper.players)
            foreach (var piece in player.Pieces)
                Alayout.Add(gui.getPieceToken(piece));

        SetHomeBlock(gui.LockHome1, "red");
        SetHomeBlock(gui.LockHome2, "green");
        SetHomeBlock(gui.LockHome3, "yellow");
        SetHomeBlock(gui.LockHome4, "blue");
        // Handle layout size changes
        Alayout.SizeChanged += (sender, e) => { 
            _unitX = Alayout.Width / 15.0;
            _unitY = Alayout.Height / 15.0;
            SyncRtcOverlayState();
            Pupulate(rotation);
        };

        RedPlayerSeat.reset();
        GreenPlayerSeat.reset();
        YellowPlayerSeat.reset();
        BluePlayerSeat.reset();

        // Refresh preferences in case they have changed

        SoundSwitch.Source = Preferences.Default.Get("IsSoundEnabled", true) ? "switch_btn_on.webp" : "switch_btn_off.webp";
        VibrationSwitch.Source = Preferences.Default.Get("IsVibrationEnabled", true) ? "switch_btn_on.webp" : "switch_btn_off.webp";

        //The Display to show selection of single or double token move
        TokenSelector.IsVisible = true;
        Alayout.Remove(TokenSelector);
        Alayout.Add(TokenSelector);

        double x = engine.EngineHelper.originalPath["p0"][1] * (Alayout.Width / 15) - (TokenSelector.Width / 2) + 10;
        double y = engine.EngineHelper.originalPath["p0"][0] * (Alayout.Height / 15) - TokenSelector.Height - 2;
     
        SyncRtcOverlayState();


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
                    PlayerPicture = playerp.PictureUrl
                });
            }

        TokenSelector.IsVisible = false;

        if (gameMode == "Client")
        {
            Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(0), () =>
            {
                ChatScrollView.InputTransparent = true;
                MessageEntryContainer.IsEnabled =
                    ChatScrollView.IsEnabled =
                    ChatScrollView.IsVisible =
                    MessageEntryContainer.IsVisible = true;
            });
            GlobalConstants.MatchMaker.ReceiveChatMessage += UpdateMessages;
        }
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
            AbsoluteLayout.SetLayoutBounds(lockHome, new Rect(0, 0, _unitX - 6, _unitY - 6));
            string PB = color.Substring(0, 1) + 51;
            double x = engine.EngineHelper.originalPath[PB][1] * _unitX;
            double y = engine.EngineHelper.originalPath[PB][0] * _unitY;
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
         //   ClientGlobalConstants.FlushOld();
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
                _ = RelocateAsync(pieces, engine.EngineHelper.players[i].Pieces[j], "");
            }

        SetHomeBlock(gui.LockHome1, "red");
        SetHomeBlock(gui.LockHome2, "green");
        SetHomeBlock(gui.LockHome3, "yellow");
        SetHomeBlock(gui.LockHome4, "blue");
    }
    public async Task RelocateHelper(List<Piece> pieces, Piece pieceClone, string playsound = "move")
    {
        engine.EngineHelper.animationBlock = true;

        uint animTime = 200;

        if (playsound != "")
           ClientGlobalConstants.hepticEngine?.PlayHapticFeedback(playsound);

        if (pieceClone.Location <= pieces[0].Location)
        {
            if (pieceClone.Location != pieces[0].Location)
                pieceClone.Jump(engine, 1, true);

            string PBC = pieceClone.getPieceBox();
            double x = engine.EngineHelper.originalPath[PBC][1] * _unitX;
            double y = engine.EngineHelper.originalPath[PBC][0] * _unitY;

            await RunAnimationAsync(pieces, x, y, animTime, "Move");
            
            if (pieceClone.Location != pieces[0].Location)
                await RelocateHelper(pieces, pieceClone, playsound);
            else
            {
                engine.EngineHelper.animationBlock = false;
                await ResizePieces();
            }
        }
        if (pieceClone.Location == 57)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("home");
        }
        while (engine.EngineHelper.animationBlock)
            await Task.Delay(20);
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
        var boardGroups = sameColorPieces.GroupBy(p => p.getPieceBox());

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
        var boardGroups = allPieces.GroupBy(piece => piece.getPieceBox());
        foreach (var boardGroup in boardGroups)
        {
            string boxKey = boardGroup.Key;
            var piecesInBox = boardGroup.ToList();

            // Retrieve the center coordinates from originalPath.
            if (!engine.EngineHelper.originalPath.TryGetValue(boxKey, out int[] boardCoords))
            {
                continue;
            }
            double centerX = boardCoords[1] * _unitX;
            double centerY = boardCoords[0] * _unitY;

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

        UpdateUI();
    }
    // Helper method to return the default image file name for a given color.
    private string GetDefaultImage(string colorLetter, string suffics)
    {
        switch (colorLetter)
        {
            case "r": return Constants.Skins.RedToken.Replace(".webp", suffics + ".webp");
            case "g": return Constants.Skins.GreenToken.Replace(".webp", suffics + ".webp");
            case "y": return Constants.Skins.YellowToken.Replace(".webp", suffics + ".webp");
            case "b": return Constants.Skins.BlueToken.Replace(".webp", suffics + ".webp");
            default: return "default.webp"; // Fallback in case no matching color is found.
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
    public async Task PlayerPieceClicked(String piece1String, String piece2String, bool SendToServer = true)
    {
        if (SendToServer && engine.processing && isInputLocked)
            return;
        TokenSelector.IsVisible = false;
        if (!engine.EngineHelper.checkTurn(piece1String, "MovePiece"))
            return;

        if (!SendToServer || piece2String != "") {
            tempPiece = null;
            await MovePiece(piece1String, piece2String, SendToServer);
            return;
        }
        try
        {
            Piece piece1 = engine.EngineHelper.currentPlayer.Pieces.Find(P => P.Name == piece1String);
            string currentBox = "";
            int ownAtBox = 0;
            
            if (engine.EngineHelper.currentPlayer.Color.ToLower().Contains(piece1String.Replace("1", "").Replace("2", "").Replace("3", "").Replace("4", "")) && (engine.EngineHelper.diceValue == 2 || engine.EngineHelper.diceValue == 4 || engine.EngineHelper.diceValue == 6))
            {
                if (piece1 != null)
                {
                    currentBox = piece1.getPieceBox();
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
    public async Task<string> MovePiece(String piece1String, String piece2String, bool SendToServer = true)
    {
        if (isInputLocked || (SendToServer && engine.processing)) 
            return "-2";
        String result = "-1";
        if (engine.EngineHelper.checkTurn(piece1String, "MovePiece"))
        {
            isInputLocked = true;
            try
            {
                if (engine.EngineHelper.gameMode == "Client" && SendToServer)
                {
                    int sendIndex = ClientGlobalConstants.game.engine.EngineHelper.index + 1;
                    int sendIndexServer = ClientGlobalConstants.game.engine.EngineHelper.indexServer + 1;

                    bool canFastStart = false;
                    if (Application.Current is App app)
                        canFastStart = app.ClientReceiver.IsServerClockPingFresh();

                    bool optimisticApplied = false;
                    if (canFastStart)
                    {
                        Console.WriteLine("[MovePiece] FastPath=True Reason=FreshServerClockPing");
                        string localFastResult = await engine.MovePieceAsync(piece1String, piece2String);
                        if (localFastResult != "," && !localFastResult.Contains("-0"))
                        {
                            result = localFastResult;
                            optimisticApplied = true;
                        }
                        else
                        {
                            Console.WriteLine("[MovePiece] FastPath local apply invalid. Falling back to authoritative response handling.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("[MovePiece] FastPath=False Reason=StaleOrMissingServerClockPing");
                    }

                    GameCommand command = new GameCommand
                    {
                        SendToClientFunctionName = "MovePiece",
                        seatName = ClientGlobalConstants.game.playerColor.ToLower(),
                        diceValue = "",
                        piece1 = piece1String,
                        piece2 = piece2String,
                        Index = sendIndex,
                        IndexServer = sendIndexServer,
                    };
                    
                    GameCommand resultCommand = await GlobalConstants.MatchMaker?.SendCommandAsync(command, "MovePiece");

                    Console.WriteLine($"[MovePiece] SendResult Null={resultCommand == null} ExpectedIndex={command.Index} ExpectedIndexServer={command.IndexServer}");
                    if (resultCommand != null && string.Equals(resultCommand.Result, "Replay", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"[MovePiece] SendResult=Replay IndexServer={resultCommand.IndexServer} Piece1={resultCommand.piece1} Piece2={resultCommand.piece2}");
                        var replayApplied = await ApplyReplayCommandAsync(resultCommand);
                        Console.WriteLine($"[MovePiece] ReplayApplied={replayApplied} IndexServer={resultCommand.IndexServer}");
                        if (!replayApplied)
                            result = "-2";
                    }
                    else if (resultCommand != null && !string.IsNullOrWhiteSpace(resultCommand.piece1))
                    {
                        Console.WriteLine($"[MovePiece] SendResult=Command Index={resultCommand.Index} IndexServer={resultCommand.IndexServer} Piece1={resultCommand.piece1} Piece2={resultCommand.piece2} OptimisticApplied={optimisticApplied}");
                        bool sameAsOptimistic = optimisticApplied && 
                            string.Equals(resultCommand.piece1, piece1String, StringComparison.Ordinal);
                        Console.WriteLine($"[MovePiece] SameAsOptimistic={sameAsOptimistic} LocalPieces={piece1String},{piece2String} ServerPieces={resultCommand.piece1},{resultCommand.piece2}");

                        string result2 = result;
                        if (!sameAsOptimistic)
                        {
                            Console.WriteLine($"[MovePiece] ApplyingAuthoritativeLocally IndexServer={resultCommand.IndexServer}");
                            result2 = await engine.MovePieceAsync(resultCommand.piece1, resultCommand.piece2);
                        }
                        else
                        {
                            Console.WriteLine("[MovePiece] Authoritative command matches optimistic move. Skipping re-apply.");
                        }

                        Console.WriteLine($"Local : {result}");
                        if (result2 == "," || result2.Contains("-0"))
                        {
                            result = "-1";
                            Console.WriteLine("Invalid move attempted.");
                        }
                        else
                        {
                            result = result2;
                            if (!_commandStore.Any(c => c.IndexServer == resultCommand.IndexServer))
                            {
                                _commandStore.Add(resultCommand);
                                Console.WriteLine($"[MovePiece] CommandCommitted AddedToStore=True IndexServer={resultCommand.IndexServer}");
                            }
                            else
                            {
                                Console.WriteLine($"[MovePiece] CommandCommitted AddedToStore=False Reason=AlreadyExists IndexServer={resultCommand.IndexServer}");
                            }

                            // Advance indices exactly once on authoritative command commit.
                            ClientGlobalConstants.game.engine.EngineHelper.index++;
                            ClientGlobalConstants.game.engine.EngineHelper.indexServer++;
                            Console.WriteLine($"[MovePiece] IndexAdvanced Index={ClientGlobalConstants.game.engine.EngineHelper.index} IndexServer={ClientGlobalConstants.game.engine.EngineHelper.indexServer}");
                        }

                        if (command.Index != resultCommand.Index)
                        {
                            Console.WriteLine("ERROR SERVER OUT OF SYNC AT PIECE");
                        }
                    }
                    else
                    {
                        result = "-2";
                        Console.WriteLine($"[MovePiece] SendResult=RejectedOrStale ExpectedIndexServer={command.IndexServer}. Waiting for pull sync.");
                    }
                }
                else
                {
                    string result2 = await engine.MovePieceAsync(piece1String, piece2String);
                    Console.WriteLine($"Local : {result}");
                    if (result2 == "," || result2.Contains("-0"))
                    {
                        result = "-1";
                        Console.WriteLine("Invalid move attempted.");
                    }
                    else
                    {
                        result = result2;                        
                        ClientGlobalConstants.game.engine.EngineHelper.index++;
                        ClientGlobalConstants.game.engine.EngineHelper.indexServer++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during MovePiece: {ex.Message}");
            }
        }
        isInputLocked = false;
        UpdateUI();
        Console.WriteLine(result);
        return result;
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
                string currentBox = tempPiece.getPieceBox();
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
    private async Task<bool> ApplyReplayCommandAsync(GameCommand command)
    {
        if (command == null || string.IsNullOrWhiteSpace(command.SendToClientFunctionName))
            return false;

        if (_commandStore.Any(c => c.IndexServer == command.IndexServer))
            return true;

        switch (command.SendToClientFunctionName)
        {
            case "MovePiece":
                if (!string.IsNullOrWhiteSpace(command.piece1) && !string.IsNullOrWhiteSpace(command.piece2))
                {
                    var moveResult = await MovePiece(command.piece1, command.piece2, false);
                    if (moveResult != "-2" && !moveResult.Contains("-1") && !moveResult.Contains("-0"))
                    {
                        _commandStore.Add(command);
                        return true;
                    }
                }
                break;
            case "DiceRoll":
                if (!string.IsNullOrWhiteSpace(command.seatName) &&
                    !string.IsNullOrWhiteSpace(command.diceValue) &&
                    command.piece1 != null &&
                    command.piece2 != null)
                {
                    var rollResult = await PlayerDiceClicked(command.seatName, command.diceValue, command.piece1, command.piece2, false);
                    if (rollResult != "-2" && !rollResult.Contains("-1") && !rollResult.Contains("-0"))
                    {
                       // _commandStore.Add(command);
                        return true;
                    }
                }
                break;
        }

        return false;
    }
    public async Task<string> PlayerDiceClicked(String SeatColor, String DiceValue, String Piece1, String Piece2, bool SendToServer = true)
    {
        if (isInputLocked || (SendToServer && engine.processing)) 
            return "-2"; // engine is busy retry in a while
        TokenSelector.IsVisible = false;
        String result = "-1";
        if (engine.EngineHelper.checkTurn(SeatColor, "RollDice"))
        {
            isInputLocked = true;
            try
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

                if (engine.EngineHelper.gameMode == "Client" && SendToServer)
                {
                    GameCommand command = new GameCommand
                    {
                        SendToClientFunctionName = "DiceRoll",
                        seatName = SeatColor,
                        diceValue = DiceValue,
                        piece1 = Piece1,
                        piece2 = Piece2,
                        Index = ClientGlobalConstants.game.engine.EngineHelper.index + 1,
                        IndexServer = ClientGlobalConstants.game.engine.EngineHelper.indexServer + 1,
                    };
                    //Applying the speed up locally to give instant feedback to the user, the server will validate and send a replay command if needed to correct any discrepancies
                    String localResult = await engine.SeatTurn(SeatColor, DiceValue, Piece1, Piece2);
                    result = localResult;
                    Console.WriteLine($"Local : {localResult}");
                    if (localResult.Contains("-1") || localResult.Contains("-0"))
                    {
                        Console.WriteLine("Invalid move attempted.");
                    }
                    else
                    {
                        _commandStore.Add(command);

                        ClientGlobalConstants.game.engine.EngineHelper.index++;
                        ClientGlobalConstants.game.engine.EngineHelper.indexServer++;
                        _ = SendDiceRollWithRetryAsync(command);
                    }
                }
                else
                {
                    result = await engine.SeatTurn(SeatColor, DiceValue, Piece1, Piece2);
                    Console.WriteLine($"Local : {result}");
                    if(result.Contains("-1")|| result.Contains("-0"))
                    {
                        Console.WriteLine("Invalid move attempted.");
                    }
                    else
                    {
                        ClientGlobalConstants.game.engine.EngineHelper.index++;
                        ClientGlobalConstants.game.engine.EngineHelper.indexServer++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Network error or exception during dice roll: {ex.Message}");
            }
            isInputLocked = false;            
        }

        UpdateUI();
        return result;
    }
    private bool TryMarkDiceSendPending(int indexServer)
    {
        lock (_pendingDiceSendLock)
        {
            return _pendingDiceSend.Add(indexServer);
        }
    }
    private void ClearDiceSendPending(int indexServer)
    {
        lock (_pendingDiceSendLock)
        {
            _pendingDiceSend.Remove(indexServer);
        }
    }
    private async Task SendDiceRollWithRetryAsync(GameCommand command, int maxAttempts = 2)
    {
        if (command == null || !TryMarkDiceSendPending(command.IndexServer))
            return;

        try
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                GameCommand? resultCommand = null;
                try
                {
                    resultCommand = await GlobalConstants.MatchMaker?.SendCommandAsync(command, "DiceRoll");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DiceRoll send attempt {attempt} failed: {ex.Message}");
                }

                if (resultCommand != null && string.Equals(resultCommand.Result, "Replay", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"DiceRoll replay received for IndexServer={command.IndexServer}. Stop retry and rely on sync/pull.");
                    return;
                }

                if (resultCommand != null && !string.IsNullOrWhiteSpace(resultCommand.seatName) && !string.IsNullOrWhiteSpace(resultCommand.diceValue))
                {
                    if (command.Index != resultCommand.Index)
                        Console.WriteLine("ERROR SERVER OUT OF SYNC AT DICEROLL");
                    return;
                }

                if (attempt < maxAttempts)
                    await Task.Delay(250 * attempt);
            }

            Console.WriteLine($"DiceRoll send gave up after {maxAttempts} attempts. Pull sync will converge state. IndexServer={command.IndexServer}");
        }
        finally
        {
            ClearDiceSendPending(command.IndexServer);
        }
    }
    public async Task<string> PlayerLeft(string seatName, bool SendToServer)
    {
        if (isInputLocked || (SendToServer && engine.processing))
            return "-2"; // engine is busy retry in a while        
            await engine.PlayerLeft(seatName);
        return "left";
    }
    public void UpdateUI()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // 1. Update Global Score
            if (engine?.EngineHelper != null)
            {
                var player = engine.EngineHelper.getPlayer(playerColor.ToLower());
                if (player != null)
                {
                    if (gameMode == "Client")
                    {
                        string score = $"Score : {player.Score}";
                        if (score != ScoreText.Text)
                            ScoreText.Text = score;
                    }
                    
                }
            }

            // 2. Synchronize Z-Index for all pieces based on current turn
            foreach (var p in engine.EngineHelper.players)
            {
                foreach (var piece in p.Pieces)
                {
                    var token = gui.getPieceToken(piece);
                    token.ZIndex = (p.Color == engine.EngineHelper.currentPlayer.Color) ? 100 : 1;
                }
            }
            TokenSelector.ZIndex = 200;

            // 3. Refresh Seat UI (Lamps, Timers, etc. handled by seats themselves)
        });
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

        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("DiceRoll");
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
        
        if(dicevalue==6)
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("ding");
        
        seat.StopDice(dicevalue);
    }
    private async void PopOverClicked(object sender, EventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        PopoverButton.ShowAttachedPopover();
    }
    private async void QuestionClicked(object sender, EventArgs e)
    {
        await Task.CompletedTask;
    }
    private void SyncRtcOverlayState()
    {
        _rtcHelper.UpdateState(
            Alayout.Width,
            Alayout.Height,
            _boardRotation,
            RedPlayerSeat?.IsVisible ?? false,
            GreenPlayerSeat?.IsVisible ?? false,
            YellowPlayerSeat?.IsVisible ?? false,
            BluePlayerSeat?.IsVisible ?? false);
    }
    private void CloseTokenSelector(object sender, EventArgs e)
    {
        if(TokenSelector.IsVisible)
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        //TokenSelector.TranslateTo(0, 0, 10, Easing.CubicIn);
        TokenSelector.IsVisible = false;
    }
    MessageBox mb = null;
    private async void ExitToLobby(object sender, EventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        try { PopoverButton.HideAttachedPopover(); } catch (Exception) { }

#if ANDROID
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity as AndroidX.AppCompat.App.AppCompatActivity;
        if (activity != null)
        {
            var dialog = new LudoClient.Platforms.Android.Popups.ExitDialogFragment(() =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (engine.EngineHelper.gameMode == "Client")
                    {
                        GlobalConstants.MatchMaker.LeaveCloseLobby();
                        try { PopoverButton.HideAttachedPopover(); } catch { }
                        engine.cleanGame();
                        ClientGlobalConstants.GoBack();
                    }
                    else
                    {
                        try { PopoverButton.HideAttachedPopover(); } catch { }
                        engine.cleanGame();
                        ClientGlobalConstants.GoBack();
                    }
                });
            });
            dialog.Show(activity.SupportFragmentManager, "ExitDialog");
        }
#else
        if (mb != null)
            return;
        if (engine.EngineHelper.gameMode == "Client")
            if (GlobalConstants.GameCost == 0)
                mb = new MessageBox("Exit", "Are you sure you want to exit?", "Your ranking will be affected!");
            else
                mb = new MessageBox("Exit", "Are you sure you want to exit?", "You will lose your bet amount!");
        else
            mb = new MessageBox("Exit", "Are you sure you want to exit?", "");

        String result = await mb.ShowAsync();
        mb = null;
        if (result == "Approve")
        {
            if (engine.EngineHelper.gameMode == "Client"){

                GlobalConstants.MatchMaker.LeaveCloseLobby();
                    engine.cleanGame();
                    ClientGlobalConstants.GoBack();
                }
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
                ClientGlobalConstants.GoBack();
            }
        }
#endif
    }
    protected override bool OnBackButtonPressed()
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        if (ChatScrollView.IsVisible)
        {
            HideChat();
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
    private void SoundSwitch_Tapped(object sender, EventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        SoundSwitch.Source = !Preferences.Default.Get("IsSoundEnabled", true) ? "switch_btn_on.webp" : "switch_btn_off.webp";
        Preferences.Default.Set("IsSoundEnabled", !Preferences.Default.Get("IsSoundEnabled", true));
    }
    private void VibrationSwitch_Tapped(object sender, EventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        VibrationSwitch.Source = !Preferences.Default.Get("IsVibrationEnabled", true) ? "switch_btn_on.webp" : "switch_btn_off.webp";
        Preferences.Default.Set("IsVibrationEnabled", !Preferences.Default.Get("IsVibrationEnabled", true));
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
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        ChatScrollView.IsVisible = true;
        ChatScrollView.InputTransparent = false;
        ChatScrollView.IsEnabled = true;
        MessageEntry.Focus();
    }
    private void HideChat_Tapped(object sender, TappedEventArgs e)
    {
        HideChat();
    }
    private void HideChat()
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        ChatScrollView.IsVisible = false;
        ChatScrollView.InputTransparent = true;
        ChatScrollView.IsEnabled = false;
        HideKeyboard();
    }
    private void OnSendButton_Tapped(object sender, TappedEventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        HideKeyboard();
        ChatScrollView.IsVisible = true;
        if (MessageEntry.Text != "")
        {
            ChatMessages cm = new();
            cm.SenderId = UserInfo.Instance.player.PlayerId;
            cm.SenderName = UserInfo.Instance.player.Name;
            cm.SenderPicture = UserInfo.Instance.player.PictureUrl;
            cm.SenderColor = this.playerColor; // Use the actual seat color (Red, Blue, etc.)
            //cm.ReceiverId = playerCard.playerID;
            //cm.ReceiverName = playerCard.playerName;
            //cm.ReceiverPicture = playerCard.playerPicture;
            cm.Message = MessageEntry.Text;
            cm.CreatedDate = DateTime.UtcNow;
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
        if (messages == null || string.IsNullOrEmpty(GlobalConstants.RoomCode)) return;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                // Only process messages for the CURRENT room
                var roomMessages = messages.Where(m => m.RoomCode == GlobalConstants.RoomCode).ToList();
                if (!roomMessages.Any()) return;

                var existingIndices = MessagesListStack.Children.OfType<ChatCard>()
                    .Select(cc => cc.Message?.Index)
                    .Where(idx => idx.HasValue)
                    .ToHashSet();

                bool added = false;
                foreach (ChatMessages cm in roomMessages)
                {
                    if (!existingIndices.Contains(cm.Index))
                    {
                        ChatCard cc = new();
                        if (UserInfo.Instance.player.PlayerId == cm.SenderId)
                            cc.SetDetails(cm, "Right", cm.SenderColor);
                        else
                            cc.SetDetails(cm, "Left", cm.SenderColor);

                        MessagesListStack.Children.Add(cc);
                        added = true;
                    }
                }

                if (added)
                {
                    // Force layout to update ContentSize
                  

                    // Auto-open chat if it was hidden
                    if (!ChatScrollView.IsVisible)
                    {
                        ChatScrollView.IsVisible = true;
                        ChatScrollView.InputTransparent = false;
                        ChatScrollView.IsEnabled = true;
                    } 
                    await Task.Delay(100);
                    await ChatScrollView.ScrollToAsync(0, 40000, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating in-game chat: {ex.Message}");
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
    //WEB ENGINE
}

