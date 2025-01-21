namespace LudoClient;

using CommunityToolkit.Maui.Views;
using LudoClient.Constants;
using LudoClient.Popups;
using Microsoft.Maui.Controls;

public partial class DashboardPage : ContentPage
{
    CashGame        cashGame = new CashGame();
    OfflinePage     offlinePage = new OfflinePage();
    PlayWithFriends playWithFriends =new PlayWithFriends();
    PracticePage    practicePage = new PracticePage();
    FriendsPage     friendsPage = new FriendsPage(); 
    public DashboardPage()
    {
        InitializeComponent();
    }
    private void CashGame_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(cashGame);
    }
    private void Offline_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(offlinePage);//Done
    }
    private void PlayWithFriend_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(playWithFriends);//Done
    }
    private void Practice_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(practicePage);//Done
        //Navigation.PushAsync(new AddCash());
    }
    private void Tournament_Clicked(object sender, EventArgs e)
    {
        if(Skins.CurrentSkin==Skins.SkinTypes.Adatiya)
        Navigation.PushAsync(cashGame);
        else
        {
            TournamentPage tournamentPage = new TournamentPage();
            Navigation.PushAsync(tournamentPage);
            _ = tournamentPage.InitializeTournamentsAsync();
        }
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