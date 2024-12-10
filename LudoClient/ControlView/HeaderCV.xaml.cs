using CommunityToolkit.Maui.Views;
using SharedCode.Constants;
using LudoClient.Popups;
namespace LudoClient;

public partial class HeaderCV : ContentView
{
    public HeaderCV()
    {
        InitializeComponent();
        player.PlayerImage = UserInfo.Instance.PictureUrl;
        Coins.Text = UserInfo.Instance.Coins+"";
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