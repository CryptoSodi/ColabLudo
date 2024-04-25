using Microsoft.Maui.Controls;
using SimpleToolkit.Core;
using Xe.AcrylicView;

namespace LudoClient.ControlView;

public partial class Popover : ContentView
{
	public Popover()
	{
        InitializeComponent();
    }
    private void ButtonClicked(object sender, EventArgs e)
    {
        var button = sender as View;
        button.ShowAttachedPopover();
    }
    private void BtnExitToLobby(object sender, EventArgs e)
    {
        // Find the parent of the button
        var parent = ((ImageButton)sender).Parent;
        // Traverse up the visual tree until you find the Popover control
        while (parent != null && !(parent is AcrylicView))
        {
            parent = ((Element)parent).Parent;
        }
        // If the parent is found and is a Popover control, hide it
        if (parent is AcrylicView popover)
        {
            popover.IsVisible = false;
        }
        //show pop up for Exit to lobby
        messageBoxCcnfirm.IsVisible = !messageBoxCcnfirm.IsVisible;
    }
}