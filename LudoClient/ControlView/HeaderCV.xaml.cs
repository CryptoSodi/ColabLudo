using CommunityToolkit.Maui.Views;
using SharedCode.Constants;
using LudoClient.Popups;
using LudoClient.Constants;
namespace LudoClient;

public partial class HeaderCV : ContentView
{
    public HeaderCV()
    {
        InitializeComponent();
        if (UserInfo.Instance.PictureBlob == "")
        {
            PlayerImageItem.Source = UserInfo.Instance.PictureUrl;
            UserInfo.DownloadImageAsBase64Async(UserInfo.Instance.PictureUrl);
        }
        else
            PlayerImageItem.Source = UserInfo.ConvertBase64ToImage(UserInfo.Instance.PictureBlob);
        Coins.Text = UserInfo.Instance.Coins + "";
    }
    private void Navigate_Settings(object sender, EventArgs e)
    {
        Application.Current?.MainPage?.ShowPopup(ClientGlobalConstants.settings);
    }
    private void OnPlayerTapped(object sender, EventArgs e)
    {
        Application.Current?.MainPage?.ShowPopup(ClientGlobalConstants.profileInfo);
    }
}