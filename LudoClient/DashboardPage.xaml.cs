namespace LudoClient;

using CommunityToolkit.Maui.Views;
using LudoClient.ControlView;
using LudoClient.CoreEngine;
using LudoClient.Popups;
using Microsoft.Maui.Controls;
using System.Security.AccessControl;

public partial class DashboardPage : ContentPage
{
    CashGame        cashGame = new CashGame();
    OfflinePage     offlinePage = new OfflinePage();
    PlayWithFriends playWithFriends =new PlayWithFriends();
    PracticePage    practicePage = new PracticePage();
    public DashboardPage()
    {
        InitializeComponent();
        Task.Run(async () =>
        {
            // Asynchronously initialize heavy content in the background
            //  await cashGame.initComponent();
        });
    }
    private void CashGame_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(cashGame);
    }
    private void Offline_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(offlinePage);
    }
    private void PlayWithFriend_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(playWithFriends);
    }
    private void Practice_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new FriendsPage());
        //Navigation.PushAsync(new AddCash());
        //Navigation.PushAsync(practicePage);
    }
    private void Tournament_Clicked(object sender, EventArgs e)
    {
        TournamentPage tournamentPage = new TournamentPage();
        Navigation.PushAsync(tournamentPage);
        _ = tournamentPage.InitializeTournamentsAsync();
    }
    private void Bonus_Clicked(object sender, EventArgs e)
    {
        this.ShowPopup(new DailyBonus());
        //this.ShowPopup(new EditInfo());
        //this.ShowPopup(new PanCardVerfication());
        //this.ShowPopup(new Results());
        //this.ShowPopup(new AddCash());
    }
}