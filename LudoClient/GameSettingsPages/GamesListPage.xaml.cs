using LudoClient.Constants;
using LudoClient.ControlView;
using LudoClient.Models;
using System.Net.Http;
using System.Text.Json;
namespace LudoClient;
public partial class GamesListPage : ContentPage
{
    public GamesListPage()
    {
        
        InitializeComponent();
        Tab1.SwitchSource = Tab1.SwitchOn;
        Tab2.SwitchSource = Tab2.SwitchOff;
    }
    public async Task InitializeTournamentsAsync()
    {
        // Retrieve or create a list of tournaments
        List<Tournament> tournaments = await GetTournamentsAsync();
        // Dynamically create and add TournamentDetailList controls
        foreach (var tournament in tournaments)
        {
            var tournamentDetail = new TournamentDetailList();
            tournamentDetail.SetTournamentDetails(
                tournamentId: tournament.TournamentId,
                tournamentName: tournament.TournamentName,
                startDate: tournament.StartDate.ToString("g"),
                endDate: tournament.EndDate.ToString("g"),
                entryPrice: tournament.EntryPrice,
                prizeAmount: tournament.PrizeAmount
            );
            // Add the control to the stack layout
            TournamentListStack.Children.Add(tournamentDetail);
        }
    }
    private async Task<List<Tournament>> GetTournamentsAsync()
    {
        HttpResponseMessage response = await GlobalConstants.httpClient.GetAsync("api/tournament");
        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var tournaments = JsonSerializer.Deserialize<List<Tournament>>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return tournaments ?? new List<Tournament>();
        }
        else
        {
            // Handle the error case as needed
            return new List<Tournament>();
        }
    }
    private void TabRequestedActivate(object sender, EventArgs e)
    {
        if (sender is ImageSwitch activeTab)
        {
            ActivateTab(activeTab);
        }
    }
    private void ActivateTab(ImageSwitch activeTab)
    {
        Tab1.SwitchSource = Tab1 == activeTab ? Tab1.SwitchOn : Tab1.SwitchOff;
        Tab2.SwitchSource = Tab2 == activeTab ? Tab2.SwitchOn : Tab2.SwitchOff;
        // Add logic here to change the content based on the active tab
    }
}