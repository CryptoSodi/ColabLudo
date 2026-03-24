namespace LudoClient;

using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using LudoClient.Constants;
using LudoClient.Popups;
using Microsoft.Maui.Controls;
using SharedCode.Constants;
using System.Diagnostics;

public partial class DashboardPage : ContentPage
{
    private bool _NavigationCooldown = false;
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
       // PracticeImage.Source = isConnected ? Skins.Practice : Skins.Practice_Gray;
        TournamentImage.Source = isConnected ? Skins.Tournament : Skins.Tournament_Gray;
        DailyBonusImage.Source = isConnected ? Skins.DailyBonus : Skins.DailyBonus_Gray;
    }
    private async void CashGame_Clicked(object sender, EventArgs e)
    {
        if (_NavigationCooldown || !GlobalConstants.MatchMaker.Connected)
            return;
        _NavigationCooldown = true;
        try
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            if (UserInfo.Instance.player.Wallet?.AvailableBalance >= GlobalConstants.initialEntry)
            {
                Navigation.PushAsync(ClientGlobalConstants.cashGame);
            }
            else
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Toast.Make("Not enough coins!", ToastDuration.Long, 24).Show();
                });
        }
        finally
        {
            await Task.Delay(500); // half-second cooldown
            _NavigationCooldown = false;
        }
    }
    private async void PlayWithFriend_Clicked(object sender, EventArgs e)
    {
        if (_NavigationCooldown || !GlobalConstants.MatchMaker.Connected)
            return;
        _NavigationCooldown = true;
        try
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            if (UserInfo.Instance.player.Wallet?.AvailableBalance >= GlobalConstants.initialEntry)
            {
                await Navigation.PushAsync(ClientGlobalConstants.playWithFriends);//Done
            }
            else
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Toast.Make("Not enough coins!", ToastDuration.Long, 24).Show();
                });
        }
        finally
        {
            await Task.Delay(500); // half-second cooldown
            _NavigationCooldown = false;
        }
    }
    private async void Tournament_Clicked(object sender, EventArgs e)
    {
        if (_NavigationCooldown || !GlobalConstants.MatchMaker.Connected)
            return;
        _NavigationCooldown = true;
        try
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            TournamentPage tournamentPage = new TournamentPage();
            await Navigation.PushAsync(tournamentPage);
        }
        finally
        {
            await Task.Delay(500); // half-second cooldown
            _NavigationCooldown = false;
        }
    }
    private async void TestUi_Clicked(object sender, EventArgs e)
    {
#if ANDROID
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity as AndroidX.AppCompat.App.AppCompatActivity;
        if (activity != null)
        {
            var dialog = new LudoClient.Platforms.Android.Popups.TestUiDialogFragment();
            dialog.Show(activity.SupportFragmentManager, "TestUiDialog");
        }
#endif
    }

    private async void Bonus_Clicked(object sender, EventArgs e)
    {
        if (_NavigationCooldown || !GlobalConstants.MatchMaker.Connected)
            return;
        _NavigationCooldown = true;
        try
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
    #if ANDROID
            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity as AndroidX.AppCompat.App.AppCompatActivity;
            if (activity != null)
            {
                var dialog = new LudoClient.Platforms.Android.Popups.DailyBonusDialogFragment();
                dialog.Show(activity.SupportFragmentManager, "DailyBonusDialog");
            }
    #else
            ClientGlobalConstants.dailyBonus.DailyBonus_Loaded(null, null);
            await this.ShowPopupAsync(ClientGlobalConstants.dailyBonus, new PopupOptions { Shape = null });
    #endif
        }
        finally
        {
            await Task.Delay(500); // half-second cooldown
            _NavigationCooldown = false;
        }
    }
}