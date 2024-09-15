namespace LudoClient;
using Microsoft.Maui.Controls;
public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();
    }
    private void CashGame_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new CashGame());
    }
    private void Offline_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new OfflinePage());
    }
    private void PlayWithFriend_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new PlayWithFriends());
    }
    private void Practice_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new PracticePage());
    }
    private void Tournament_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new TournamentPage());
    }
    private void Bonus_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new DailyBonusPage());
    }
}