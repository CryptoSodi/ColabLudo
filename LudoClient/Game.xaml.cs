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
        Gui gui = new Gui(red1,red2,red3,red4);
        Engine = new Engine(gui);
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