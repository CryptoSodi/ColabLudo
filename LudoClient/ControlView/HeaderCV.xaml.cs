using CommunityToolkit.Maui.Views;
using LudoClient.Popups;
namespace LudoClient;

public partial class HeaderCV : ContentView
{
    public HeaderCV()
    {
        InitializeComponent();
    }
    private void Navigate_Settings(object sender, EventArgs e)
    {
        Application.Current.MainPage.ShowPopup(new Settings());
    }
    private void OnPlayerTapped(object sender, EventArgs e)
    {
       // Navigation.PushAsync(new ProfilePage());
        
        Application.Current.MainPage.ShowPopup(new ProfileInfo());
    }
}