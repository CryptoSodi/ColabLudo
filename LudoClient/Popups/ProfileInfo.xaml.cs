using Microsoft.AspNetCore.SignalR.Client;
using SharedCode;
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
            player.playerImageItem.Source = UserInfo.ConvertBase64ToImage(UserInfo.Instance.PictureUrlBlob);
            player.PlayerName = UserInfo.Instance.player.Name;
            Email.Text = UserInfo.Instance.player.Email;
            Number.Text = UserInfo.Instance.player.PhoneNumber;
            Location.Text = UserInfo.Instance.player.City;

            C1.setValue(UserInfo.Instance.player.GamesPlayed + "");
            C2.setValue(UserInfo.Instance.player.GamesWon + "");
            C3.setValue(UserInfo.Instance.player.GamesLost + "");
            C4.setValue(UserInfo.Instance.player.BestWin + "");
            C5.setValue(UserInfo.Instance.player.TotalWin + "");
            C6.setValue(UserInfo.Instance.player.TotalLost + "");
            player.SetScore(UserInfo.Instance.player.Score, UserInfo.Instance.player.PhoneNumber != "###########");
            loadValues();
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
            C4.setValue(dto.BestWin + "");
            C5.setValue(dto.TotalWin + "");
            C6.setValue(dto.TotalLost + "");
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
}