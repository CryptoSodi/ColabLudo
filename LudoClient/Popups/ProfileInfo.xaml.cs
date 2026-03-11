using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using LudoClient.Constants;
using SharedCode.Constants;

namespace LudoClient.Popups;

public partial class ProfileInfo : BasePopup
{
    public ProfileInfo()
    {
        InitializeComponent();
        //reload this if pictureblock is ""

        MainThread.BeginInvokeOnMainThread(() =>
        {
            player.playerImageItem.Source = UserInfo.Instance.ProfileImageSource;
            player.PlayerName = UserInfo.Instance.player.Name;
            Email.Text = UserInfo.Instance.player.Email;
            Number.Text = UserInfo.Instance.player.PhoneNumber;
            Location.Text = UserInfo.Instance.player.City;

            C1.setValue(UserInfo.Instance.player.GamesPlayed + "");
            C2.setValue(UserInfo.Instance.player.GamesWon + "");
            C3.setValue(UserInfo.Instance.player.GamesLost + "");
            C4.setValue(ClientGlobalConstants.NormalizeCoins(UserInfo.Instance.player.BestWin));
            C5.setValue(ClientGlobalConstants.NormalizeCoins(UserInfo.Instance.player.TotalWin));
            C6.setValue(ClientGlobalConstants.NormalizeCoins(UserInfo.Instance.player.TotalLost));
            player.SetScore(UserInfo.Instance.player.Score, UserInfo.Instance.player.PhoneNumber != "###########");
            loadValues();

            if (GlobalConstants.MatchMaker.Connected)
            {
                string result = GlobalConstants.MatchMaker.MintNFT(0).GetAwaiter().GetResult();

                if (string.IsNullOrWhiteSpace(result))
                {
                    return;
                }
                if (result.Contains("Success"))
                {
                    result = result.Replace(",Success", "");
                    string[] ids = result.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    Coins.Text = ids.Length + " NFTS";
                }
            }
            else
            {

            }
        });
    }
    public async void loadValues()
    {
        try
        {
            PlayerInfo dto = await GlobalConstants.MatchMaker.UserConnectedSetID();
            C1.setValue(dto.GamesPlayed + "");
            C2.setValue(dto.GamesWon + "");
            C3.setValue(dto.GamesLost + "");
            C4.setValue(ClientGlobalConstants.NormalizeCoins(dto.BestWin));
            C5.setValue(ClientGlobalConstants.NormalizeCoins(dto.TotalWin));
            C6.setValue(ClientGlobalConstants.NormalizeCoins(dto.TotalLost));
            if (dto.PhoneNumber != null)
            {
                Preferences.Set(nameof(UserInfo.Instance.player.PhoneNumber), dto.PhoneNumber);
                Preferences.Set(nameof(UserInfo.Instance.player.Score), dto.Score);
                player.SetScore(dto.Score, true);
                Number.Text = dto.PhoneNumber;
            }
            else
            {
                Preferences.Set(nameof(UserInfo.Instance.player.Score), dto.Score);
                player.SetScore(dto.Score, false);
            }
            //Score.setValue(dto.Score + "");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
    private void OnManageNftsTapped(object sender, EventArgs e)
    {
        if (!GlobalConstants.MatchMaker.Connected)
            return;
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        ClientGlobalConstants.mintingPage = new MintingPage();
        ClientGlobalConstants.dashBoard.ShowPopup(ClientGlobalConstants.mintingPage, new PopupOptions { Shape = null });
    }
}