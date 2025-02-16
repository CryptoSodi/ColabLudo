namespace LudoClient;

using CommunityToolkit.Maui.Views;
using LudoClient.Constants;
using LudoClient.Popups;
using Microsoft.Maui.Controls;
using System.Diagnostics;

public partial class DashboardPage : ContentPage
{
    
    public DashboardPage()
    {
        InitializeComponent();
        ClientGlobalConstants.Init();
        ClientGlobalConstants.dashBoard = this;
        
    }
    private void CashGame_Clicked(object sender, EventArgs e)
    {
        var stopwatch = Stopwatch.StartNew();
        Navigation.PushAsync(ClientGlobalConstants.cashGame).Wait(); // For testing only
        Console.WriteLine($"Navigation took: {stopwatch.ElapsedMilliseconds}ms");
    }
    private void Offline_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(ClientGlobalConstants.offlinePage);//Done
    }
    private void PlayWithFriend_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(ClientGlobalConstants.playWithFriends);//Done
    }
    private void Practice_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(ClientGlobalConstants.practicePage);//Done
        //Navigation.PushAsync(new AddCash());
    }
    private void Tournament_Clicked(object sender, EventArgs e)
    {
        if (Skins.CurrentSkin == Skins.SkinTypes.Adatiya)
            Navigation.PushAsync(ClientGlobalConstants.cashGame);
        else
        {
            TournamentPage tournamentPage = new TournamentPage();
            Navigation.PushAsync(tournamentPage);
            _ = tournamentPage.InitializeTournamentsAsync();
        }
    }
    private void Bonus_Clicked(object sender, EventArgs e)
    {
       // dailyBonus.IsVisible = !dailyBonus.IsVisible;
        // profileInfo.IsVisible = !profileInfo.IsVisible;
        // Navigation.PushAsync(new Settings());
        this.ShowPopup(ClientGlobalConstants.dailyBonus());
        //this.ShowPopup(new EditInfo());
        //this.ShowPopup(new PanCardVerfication());
        //this.ShowPopup(new Results());
        //this.ShowPopup(new AddCash());
    }
}