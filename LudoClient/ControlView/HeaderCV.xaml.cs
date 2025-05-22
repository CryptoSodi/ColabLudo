using CommunityToolkit.Maui.Views;
using SharedCode.Constants;
using LudoClient.Popups;
using LudoClient.Constants;
using System.Net;
using SharedCode;
namespace LudoClient;

public partial class HeaderCV : ContentView
{
    private System.Timers.Timer _qrCodeTimer;
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

        // Initialize and start the timer
        _qrCodeTimer = new System.Timers.Timer(30000); // 60,000 milliseconds = 60 seconds
        _qrCodeTimer.Elapsed += async (sender, e) => await GenerateQRCodeAsync();
        _qrCodeTimer.AutoReset = true;
        _qrCodeTimer.Enabled = true;

        GenerateQRCodeAsync();
    }
    public async Task GenerateQRCodeAsync()
    {
        if (GlobalConstants.MatchMaker != null)
        {
            try
            {
                DepositInfo info = GlobalConstants.MatchMaker.UserConnectedSetID().GetAwaiter().GetResult();
                // You can tweak these hex colors and size as you like:
                // Update the image source asynchronously (UI thread)
                UserInfo.Instance.Address = info.Address;
                UserInfo.Instance.SolBalance = Double.Parse(info.SolBalance);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Coins.Text = info.SolBalance + " SOL";
                });
            }
            catch (Exception)
            {

            }
        }
        else
        {
          await Task.Delay(100);
          GenerateQRCodeAsync();
        }
    }
    private void Navigate_Settings(object sender, EventArgs e)
    {
        ClientGlobalConstants.settings = new Settings();
        Application.Current?.MainPage?.ShowPopup(ClientGlobalConstants.settings);
    }
    private void OnPlayerTapped(object sender, EventArgs e)
    {
        //Application.Current?.MainPage?.ShowPopup(ClientGlobalConstants.profileInfo);
        //ClientGlobalConstants.profileInfo = new ProfileInfo();
    }
}