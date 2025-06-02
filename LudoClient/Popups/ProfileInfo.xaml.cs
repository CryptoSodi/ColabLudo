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
            player.PlayerName = UserInfo.Instance.Name;
            Email.Text = UserInfo.Instance.Email;
            Number.Text = UserInfo.Instance.PhoneNumber;
            Location.Text = UserInfo.Instance.City;

            C1.setValue(UserInfo.Instance.GamesPlayed + "");
            C2.setValue(UserInfo.Instance.GamesWon + "");
            C3.setValue(UserInfo.Instance.GamesLost + "");
            C4.setValue(UserInfo.Instance.BestWin + "");
            C5.setValue(UserInfo.Instance.TotalWin + "");
            C6.setValue(UserInfo.Instance.TotalLost + "");
            player.SetScore(UserInfo.Instance.Score, UserInfo.Instance.PhoneNumber != "###########");
            loadValues();
        });
    }
    public async void loadValues()
    {
        // ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        try
        {
            StateInfo dto = await GlobalConstants.MatchMaker._hubConnection.InvokeAsync<StateInfo>("LoadPlayerData", UserInfo.Instance.Id);
            C1.setValue(dto.GamesPlayed + "");
            C2.setValue(dto.GamesWon + "");
            C3.setValue(dto.GamesLost + "");
            C4.setValue(dto.BestWin + "");
            C5.setValue(dto.TotalWin + "");
            C6.setValue(dto.TotalLost + "");
            if (dto.PhoneNumber != null)
            {
                Preferences.Set(nameof(UserInfo.Instance.PhoneNumber), dto.PhoneNumber);
                Preferences.Set(nameof(UserInfo.Instance.Score), dto.Score);
                player.SetScore(dto.Score, true);
                Number.Text = dto.PhoneNumber;
            }
            else
            {
                Preferences.Set(nameof(UserInfo.Instance.Score), dto.Score);
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