using LudoClient.Constants;
using LudoClient.ControlView;
using LudoClient.Models;
using System.Text.Json;
namespace LudoClient;
public partial class TournamentPage : ContentPage
{
    private readonly HttpClient _httpClient;
    public TournamentPage()
    {
        InitializeComponent();
        Tab1.SwitchSource = Tab1.SwitchOn;
        Tab2.SwitchSource = Tab2.SwitchOff;
        _httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:7255/") }; // Replace with your API base URL
    }
    public async Task InitializeTournamentsAsync()
    {
        // Retrieve or create a list of tournaments
        List<Tournament> tournaments;
        if(GlobalConstants.Debug)
            tournaments = GetTournaments();
        else
            tournaments = await GetTournamentsAsync();
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
    private List<Tournament> GetTournaments()
    {
        // Sample data. Replace with actual data retrieval logic.
        return new List<Tournament>
            {
                new Tournament
                {
                    TournamentId = 0,
                    TournamentName = "Monthly Tournament",
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(1),
                    EntryPrice = 100,
                    PrizeAmount = 1000
                },
                new Tournament
                {
                    TournamentId = 1,
                    TournamentName = "Weekly Championship",
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(7),
                    EntryPrice = 50,
                    PrizeAmount = 500
                },
                // Add more tournaments as needed
            };
    }
    private async Task<List<Tournament>> GetTournamentsAsync()
    {
        var response = await _httpClient.GetAsync($"api/tournament");
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