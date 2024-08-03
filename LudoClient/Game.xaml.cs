using LudoClient.ControlView;
using LudoClient.CoreEngine;
using SimpleToolkit.Core;
using Xe.AcrylicView;

namespace LudoClient;

public partial class Game : ContentPage
{
    Engine Engine;

    public Game()
    {
        InitializeComponent();
        Gui gui = new Gui(red1, red2, red3, red4, gre1, gre2, gre3, gre4, blu1, blu2, blu3, blu4, yel1, yel2, yel3, yel4, RedPlayerSeat, GreenPlayerSeat, YellowPlayerSeat, BluePlayerSeat);
        Engine = new Engine(gui);
        //Event Handelers
        GreenPlayerSeat.OnDiceClicked += PlayerDiceClicked;
        YellowPlayerSeat.OnDiceClicked += PlayerDiceClicked;
        RedPlayerSeat.OnDiceClicked += PlayerDiceClicked;
        BluePlayerSeat.OnDiceClicked += PlayerDiceClicked;
        red1.OnPieceClicked += PlayerPieceClicked;
    }
    private void PlayerPieceClicked(String PieceName)
    {
        // Handle the dice click for the green player
    }

    private void PlayerDiceClicked(String SeatName)
    {
        // Handle the dice click for the green player
         Engine.RollDice();
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