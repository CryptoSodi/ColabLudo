using SimpleToolkit.Core;
using Xe.AcrylicView;

namespace LudoClient;

public partial class Game : ContentPage
{
	public Game()
	{
		InitializeComponent();
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