namespace LudoClient;
using Microsoft.Maui.Controls;
public partial class DashboardPage : ContentPage
{
    CashGame        cashGame = new CashGame();
    OfflinePage     offlinePage = new OfflinePage();
    PlayWithFriends playWithFriends =new PlayWithFriends();
    PracticePage    practicePage = new PracticePage();
    TournamentPage  tournamentPage = new TournamentPage();
    DailyBonusPage  dailyBonusPage = new DailyBonusPage();
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
        Navigation.PushAsync(offlinePage);
    }
    private void PlayWithFriend_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(playWithFriends);
    }
    private void Practice_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(practicePage);
    }
    private void Tournament_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(tournamentPage);
        _ = tournamentPage.InitializeTournamentsAsync();
    }
    private void Bonus_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(dailyBonusPage);
    }
}