namespace LudoClient;

using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using LudoClient.Constants;
using LudoClient.Popups;
using Microsoft.Maui.Controls;
using SharedCode.Constants;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();
        ClientGlobalConstants.dashBoard = this;

        Task.Run(async () =>
        {
            while (GlobalConstants.MatchMaker == null)
                await Task.Delay(50);

            UpdateButtons(GlobalConstants.MatchMaker.Connected);
            GlobalConstants.MatchMaker.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(GlobalConstants.MatchMaker.Connected))
                    MainThread.BeginInvokeOnMainThread(() =>
                        UpdateButtons(GlobalConstants.MatchMaker.Connected));
            };
        });
    }
    void UpdateButtons(bool isConnected)
    {
        CashImage.Source = isConnected ? Skins.Cash : Skins.Cash_Gray;
        PlayWithFriendsImage.Source = isConnected ? Skins.Play : Skins.Play_Gray;
        PracticeImage.Source = isConnected ? Skins.Practice : Skins.Practice_Gray;
        TournamentImage.Source = isConnected ? Skins.Tournament : Skins.Tournament_Gray;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Delay the heavy initialization to allow the page to render first.
        //await Task.Delay(100); // Adjust delay as needed
        //ClientGlobalConstants.Init();
    }
    private void CashGame_Clicked(object sender, EventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        if (!GlobalConstants.MatchMaker.Connected)
            return;
        if(UserInfo.Instance.LudoCoins >= GlobalConstants.initialEntry)
        {
            ClientGlobalConstants.cashGame = new CashGame();
            Navigation.PushAsync(ClientGlobalConstants.cashGame).Wait();
        }   
        else
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Toast.Make("Not enough balance!", ToastDuration.Long, 24).Show();
            });
        
    }
    private void Offline_Clicked(object sender, EventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        ClientGlobalConstants.offlinePage = new OfflinePage();
        Navigation.PushAsync(ClientGlobalConstants.offlinePage);//Done
    }
    private void PlayWithFriend_Clicked(object sender, EventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        if (!GlobalConstants.MatchMaker.Connected)
            return;
        if (UserInfo.Instance.LudoCoins >= GlobalConstants.initialEntry)
        {
            ClientGlobalConstants.playWithFriends = new PlayWithFriends();
            Navigation.PushAsync(ClientGlobalConstants.playWithFriends);//Done
        }   
        else
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Toast.Make("Not enough balance!", ToastDuration.Long, 24).Show();
            });
    }
    private void Practice_Clicked(object sender, EventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        if (!GlobalConstants.MatchMaker.Connected)
            return;
        ClientGlobalConstants.practicePage = new PracticePage();
        Navigation.PushAsync(ClientGlobalConstants.practicePage);//Done
    }
    private void Tournament_Clicked(object sender, EventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        if (!GlobalConstants.MatchMaker.Connected)
            return;
        if (Skins.CurrentSkin == Skins.SkinTypes.Adatiya)
        {
            ClientGlobalConstants.cashGame = new CashGame();
            Navigation.PushAsync(ClientGlobalConstants.cashGame);
        }   
        else
        {
            TournamentPage tournamentPage = new TournamentPage();
            Navigation.PushAsync(tournamentPage);
        }
    }
    private void Bonus_Clicked(object sender, EventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        ClientGlobalConstants.dailyBonus = new DailyBonus();
        this.ShowPopup(ClientGlobalConstants.dailyBonus);
    }
}