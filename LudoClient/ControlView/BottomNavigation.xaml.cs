namespace LudoClient.ControlView;

public partial class BottomNavigation : ContentView
{
	public BottomNavigation()
	{
		InitializeComponent();
	}

    async void OnBackButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}