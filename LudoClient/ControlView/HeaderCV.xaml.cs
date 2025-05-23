using CommunityToolkit.Maui.Views;
using LudoClient.Constants;
using LudoClient.Popups;
using SharedCode;
using SharedCode.Constants;
namespace LudoClient;

public partial class HeaderCV : ContentView
{
    private System.Timers.Timer _qrCodeTimer;
    public HeaderCV()
    {
        InitializeComponent();

        loadHeaderImage();

        // Initialize and start the timer
        _qrCodeTimer = new System.Timers.Timer(30000); // 60,000 milliseconds = 60 seconds
        _qrCodeTimer.Elapsed += async (sender, e) => await UpdateBalance();
        _qrCodeTimer.AutoReset = true;
        _qrCodeTimer.Enabled = true;

        UpdateBalance();
    }
    private async void loadHeaderImage()
    {
        if (UserInfo.Instance.PictureBlob == "")
        {
            //PlayerImageItem.Source = UserInfo.Instance.PictureUrl;
            await UserInfo.DownloadImageAsBase64Async(UserInfo.Instance.PictureUrl);
        }
        if (UserInfo.Instance.PictureBlob == "")
        {
            Console.WriteLine("BLOB NOT LOADED");
        }
        MainThread.BeginInvokeOnMainThread(() =>
        {
            PlayerImageItem.Source = UserInfo.ConvertBase64ToImage(UserInfo.Instance.PictureBlob);
        });
    }
    public async Task UpdateBalance()
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
          UpdateBalance();
        }
    }
    private void Navigate_Settings(object sender, EventArgs e)
    {
        ClientGlobalConstants.settings = new Settings();
        Application.Current?.MainPage?.ShowPopup(ClientGlobalConstants.settings);
    }
    private void OnPlayerTapped(object sender, EventArgs e)
    {
        ClientGlobalConstants.profileInfo = new ProfileInfo();
        Application.Current?.MainPage?.ShowPopup(ClientGlobalConstants.profileInfo);
    }
}