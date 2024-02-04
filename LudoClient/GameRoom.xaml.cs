namespace LudoClient;

public partial class GameRoom : ContentPage
{
	public GameRoom()
	{
		InitializeComponent();
        NavigationPage.SetHasBackButton(this, false);
    }
    protected override bool OnBackButtonPressed()
    {
        // Prevent back navigation
        return true;
    }
}