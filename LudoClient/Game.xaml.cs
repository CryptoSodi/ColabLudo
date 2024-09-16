using LudoClient.CoreEngine;
using SimpleToolkit.Core;

namespace LudoClient;

public partial class Game : ContentPage
{
    Engine Engine;
    public Game()
    {
        InitializeComponent();
        //   Grid.SetRow(GameView, 0);
        //   Grid.SetColumn(GameView, 0);

        Gui gui = new Gui(red1, red2, red3, red4, gre1, gre2, gre3, gre4, blu1, blu2, blu3, blu4, yel1, yel2, yel3, yel4, RedPlayerSeat, GreenPlayerSeat, YellowPlayerSeat, BluePlayerSeat);
        Engine = new Engine(gui, Glayout, Alayout);
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
        Engine.StopDice += new Engine.CallbackEventHandler(StopDice);
        RedPlayerSeat.reset();
        GreenPlayerSeat.reset();
        YellowPlayerSeat.reset();
        BluePlayerSeat.reset();
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
    private void ExitToLobby(object sender, EventArgs e)
    {
        PopoverButton.HideAttachedPopover();
        //show pop up for Exit to lobby
        messageBoxCcnfirm.IsVisible = !messageBoxCcnfirm.IsVisible;
    }
}