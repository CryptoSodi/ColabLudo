using LudoClient.Constants;
using SharedCode.Constants;

namespace LudoClient.ControlView;

public partial class BottomNavigation : ContentView
{
    public BottomNavigation()
    {
        InitializeComponent();
    }
    async void OnBackButtonClicked(object sender, EventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        GlobalConstants.MatchMaker.LeaveCloseLobby();
        await Navigation.PopAsync();
    }
}