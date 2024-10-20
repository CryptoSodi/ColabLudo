namespace LudoClient.ControlView;

public partial class BottomNavigation : ContentView
{
    public BottomNavigation()
    {
        InitializeComponent();
        //ImageSwitchControl.SwitchState = Preferences.Get(PreferencesKey, true);
    }

    async void OnBackButtonClicked(object sender, EventArgs e)
    {
        //await Navigation.PopAsync();
        Application.Current.MainPage = new AppShell();
    }
}