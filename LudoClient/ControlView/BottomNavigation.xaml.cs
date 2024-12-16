using SharedCode.Constants;

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
        GlobalConstants.MatchMaker.LeaveCloseLobby(UserInfo.Instance.Id);
        //await Navigation.PopAsync();
        Application.Current.MainPage = new AppShell();
    }
}