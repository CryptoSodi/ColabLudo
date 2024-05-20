using LudoClient.CoreEngine;
using SimpleToolkit.Core;
using Xe.AcrylicView;

namespace LudoClient;

public partial class Game : ContentPage
{
    Engine Engine = new Engine();
    public Game()
	{
		InitializeComponent();
        red1.location = 1;
        Grid.SetRow(red1, 10);
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