using SharedCode.Constants;
using LudoClient.ControlView;
using LudoClient.Models;
using System.Text.Json;
using LudoClient.Constants;
namespace LudoClient;
public partial class TournamentPage : ContentPage
{
    public TournamentPage()
    {
        InitializeComponent();
        Tab1.SwitchSource = Tab1.SwitchOn;
        Tab2.SwitchSource = Tab2.SwitchOff;
        Tab3.SwitchSource = Tab3.SwitchOff;
    }
    public async Task InitializeTournamentsAsync()
    {
        // Clear old items to avoid duplicates
        TournamentListStack.Children.Clear();

        List<Tournament> tournaments = await GetTournamentsAsync();

        foreach (var tournament in tournaments)
        {
            var tournamentDetail = new TournamentDetailList();
            // Pass the tournament data + status
            tournamentDetail.SetTournamentDetails(tournament);
            TournamentListStack.Children.Add(tournamentDetail);
        }
    }
    private async Task<List<Tournament>> GetTournamentsAsync()
    {
        String type = "Running"; // Default type to fetch running tournaments
        HttpResponseMessage response = await GlobalConstants.httpClient.GetAsync($"api/tournament/type/{type}");

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
            // Handle the error case as needed (logging, message, etc.)
            return new List<Tournament>();
        }
    }
    private void TabRequestedActivate(object sender, EventArgs e)
    {
        if (sender is ImageSwitch activeTab)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            Tab1.SwitchSource = Tab1 == activeTab ? Tab1.SwitchOn : Tab1.SwitchOff;
            Tab2.SwitchSource = Tab2 == activeTab ? Tab2.SwitchOn : Tab2.SwitchOff;
            Tab3.SwitchSource = Tab3 == activeTab ? Tab3.SwitchOn : Tab3.SwitchOff;
            // Add logic here to change the content based on the active tab
        }
    }
}