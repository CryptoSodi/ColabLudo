using LudoClient.ControlView;
using SimpleToolkit.Core;
namespace LudoClient.CoreEngine;
public partial class Game : ContentPage
{
    Engine Engine; 
    PlayerSeat RedPlayerSeat; PlayerSeat GreenPlayerSeat; PlayerSeat YellowPlayerSeat; PlayerSeat BluePlayerSeat;
    public Game(string gametype, string playerCount, string playerColor)
    {
        InitializeComponent();
        //Grid.SetRow(GameView, 0);
        //Grid.SetColumn(GameView, 0);

        // Create RedPlayerSeat
        RedPlayerSeat = new PlayerSeat
        {
            PlayerBG = "red_container.png",
            HorizontalOptions = LayoutOptions.FillAndExpand,
            VerticalOptions = LayoutOptions.End
        };
        // Create GreenPlayerSeat
        GreenPlayerSeat = new PlayerSeat
        {
            PlayerBG = "green_container.png",
            HorizontalOptions = LayoutOptions.FillAndExpand,
            VerticalOptions = LayoutOptions.End
        };
        // Create YellowPlayerSeat
        YellowPlayerSeat = new PlayerSeat
        {
            PlayerBG = "yellow_container.png",
            HorizontalOptions = LayoutOptions.FillAndExpand,
            VerticalOptions = LayoutOptions.End
        };
        // Create BluePlayerSeat
        BluePlayerSeat = new PlayerSeat
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

                        Row1.Children.Add(GreenPlayerSeat);
                        Row1.Children.Add(RedPlayerSeat);
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
        Gui gui = new Gui(red1, red2, red3, red4, gre1, gre2, gre3, gre4, blu1, blu2, blu3, blu4, yel1, yel2, yel3, yel4, RedPlayerSeat, GreenPlayerSeat, YellowPlayerSeat, BluePlayerSeat);
        Engine = new Engine(gametype, playerCount, playerColor, gui, Glayout, Alayout);
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

        Engine.StopDice += new Engine.CallbackEventHandler(StopDice);
    }
    private void PlayerPieceClicked(String PieceName)
    {
        //start animation
        // Handle the dice click for the green player
        Engine.MovePieceAsync(PieceName);
        //stop animmation
    }

    private void PlayerDiceClicked(String SeatName)
    {
        if (Engine.checkTurn(SeatName, "RollDice"))
        {
            RedPlayerSeat.reset();
            GreenPlayerSeat.reset();
            YellowPlayerSeat.reset();
            BluePlayerSeat.reset();

            // Handle the dice click for the green player
            //check turn
            var seat = RedPlayerSeat;
            if (SeatName == "red")
                seat = RedPlayerSeat;
            if (SeatName == "green")
                seat = GreenPlayerSeat;
            if (SeatName == "yellow")
                seat = YellowPlayerSeat;
            if (SeatName == "blue")
                seat = BluePlayerSeat;
            seat.AnimateDice();
            Engine.SeatTurn(SeatName);
        }
        //Engine.PlayGame();
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
        GameRecorder.SaveGameHistory();
        Application.Current.MainPage = new AppShell();
    }
}