
using LudoClient.ControlView;
using System.Text.Json;
using SharedCode.Constants;
using SharedCode.CoreEngine;
using SharedCode.Network;
using SimpleToolkit.Core;
using System.Security.AccessControl;
namespace LudoClient.CoreEngine;

public class PlayerDto
{
    public int PlayerId { get; set; }
    public string? PlayerName { get; set; }
    public string? PlayerPicture { get; set; }
    public string? PlayerColor { get; set; }
}
public partial class Game : ContentPage
{
    Engine Engine;
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
    
    public Game(string GameType, string seatsData)
    {
        seats = JsonSerializer.Deserialize<List<PlayerDto>>(seatsData);
        var player = seats?.FirstOrDefault(p => p.PlayerId == UserInfo.Instance.Id);
        Build("Online", GameType, player.PlayerColor);
    }
    public Game(string gameType, string playerCount, string playerColor)
    {
        Build(gameType, playerCount, playerColor);
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
        catch (Exception e) { 
            
        }
    }

    private void Build(string gameType, string playerCount, string playerColor)
    {
        InitializeComponent();
        //GlobalConstants.MatchMaker.RecievedRequest += new Client.CallbackRecievedRequest(RecievedRequest);//For ggetting msggs from the game server
        //Grid.SetRow(GameView, 0);
        //Grid.SetColumn(GameView, 0);

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
        }
        else
        {
            foreach (var (color, seat) in colors)
                if (playerColor != color)
                    if (gameType == "Computer")
                        seat.hideAuto($"Computer {Array.IndexOf(colors, (color, seat)) + 1}", "player.png", true, true);
                    else
                        seat.showAuto($"Player {Array.IndexOf(colors, (color, seat)) + 1}", "player.png", false, false);

            playerSeat.showAuto(UserInfo.Instance.Name, UserInfo.Instance.PictureUrl, false, false);
        }

        Engine = new Engine(gameType, playerCount, playerColor);
        gui.red.EngineHelper = Engine.EngineHelper;
        gui.green.EngineHelper = Engine.EngineHelper;
        gui.yellow.EngineHelper = Engine.EngineHelper;
        gui.blue.EngineHelper = Engine.EngineHelper;
        if (!Engine.EngineHelper.stopAnimate)
            StartProgressAnimation(Engine.EngineHelper.currentPlayer.Color);

        Engine.StopDice += new Engine.CallbackEventHandler(StopDice);
        Engine.AnimateDice += new Engine.Callback_AnimateDice_EventHandler(AnimateDice);
        Engine.StartProgressAnimation += new Engine.CallbackEventHandlerStartProgressAnimation(StartProgressAnimation);
        Engine.StopProgressAnimation += new Engine.CallbackEventHandlerStopProgressAnimation(StopProgressAnimation);
        Engine.RelocateAsync += new Engine.CallbackEventHandlerRelocateAsync(RelocateAsync);

        // Set rotation based on player color
        int rotation = Engine.EngineHelper.SetRotation(playerColor);
        Glayout.RotateTo(rotation);

        foreach (var player in Engine.EngineHelper.players)
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
            seat.TimerTimeout += Engine.TimerTimeoutAsync;

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

    

    public void Pupulate(int rotation)
    {
        for (int i = 0; i < Engine.EngineHelper.players.Count; i++)
            for (int j = 0; j < Engine.EngineHelper.players[i].Pieces.Count; j++)
            {
                gui.getPieceToken(Engine.EngineHelper.players[i].Pieces[j]).RotateTo(-rotation);
                AbsoluteLayout.SetLayoutBounds(gui.getPieceToken(Engine.EngineHelper.players[i].Pieces[j]), new Rect(0, 0, (Alayout.Width / 15), (Alayout.Height / 15)));
                _ = RelocateAsync(Engine.EngineHelper.players[i].Pieces[j], Engine.EngineHelper.players[i].Pieces[j]);
            }
    }
    public async Task RelocateAsync(Piece piece, Piece pieceClone)
    {
        Engine.EngineHelper.animationBlock = true;

        uint animTime = 200;
        if (Engine.EngineHelper.stopAnimate)
        {
            animTime = 40;
            pieceClone = piece.Clone();
        }
        //piece.Position
        //player.StartPosition
        string PB = Engine.EngineHelper.getPieceBox(piece);

        if (pieceClone.Location < piece.Location)
        {
            pieceClone.Jump(Engine, 1, true);
            string PBC = Engine.EngineHelper.getPieceBox(pieceClone);

            double x = Engine.EngineHelper.originalPath[PBC][1] * (Alayout.Width / 15);
            double y = Engine.EngineHelper.originalPath[PBC][0] * (Alayout.Height / 15);

            _ = gui.getPieceToken(piece).TranslateTo(x, y, animTime, Easing.CubicIn).ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    if (pieceClone.Location != piece.Location)
                        _ = RelocateAsync(piece, pieceClone);
                    else
                        Engine.EngineHelper.animationBlock = false;
                }
                else if (t.IsFaulted)
                {
                    Console.WriteLine($"Error during translation: {t.Exception?.Message}");
                }
            });
        }
        else
        {
            double x = Engine.EngineHelper.originalPath[PB][1] * (Alayout.Width / 15);
            double y = Engine.EngineHelper.originalPath[PB][0] * (Alayout.Height / 15);
            _ = gui.getPieceToken(piece).TranslateTo(x, y, animTime, Easing.CubicIn);
            Engine.EngineHelper.animationBlock = false;
        }
        while (Engine.EngineHelper.animationBlock)
        {
            await Task.Delay(20);
        }
    }
    private void PlayerPieceClicked(String PieceName)
    {
        //start animation
        // Handle the dice click for the green player

        _ = Engine.MovePieceAsync(PieceName);
        //stop animmation
    }
    private void PlayerDiceClicked(String SeatName)
    {
        if (Engine.EngineHelper.checkTurn(SeatName, "RollDice"))
        {
            gui.red.reset();
            gui.green.reset();
            gui.yellow.reset();
            gui.blue.reset();

            // Handle the dice click for the green player
            //check turn
            var seat = gui.red;
            if (SeatName == "red")
                seat = gui.red;
            if (SeatName == "green")
                seat = gui.green;
            if (SeatName == "yellow")
                seat = gui.yellow;
            if (SeatName == "blue")
                seat = gui.blue;

            seat.AnimateDice();
            Engine.SeatTurn(SeatName);
        }
        foreach (var piece in Engine.EngineHelper.currentPlayer.Pieces)
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
    private void ExitToLobby(object sender, EventArgs e)
    {
        //this.ShowPopup(new MessageBox());
        PopoverButton.HideAttachedPopover();
        //show pop up for Exit to lobby
        // messageBoxCcnfirm.IsVisible = !messageBoxCcnfirm.IsVisible;
       // GameRecorder.SaveGameHistory();
        Engine.cleanGame();
        Application.Current.MainPage = new AppShell();
    }
}