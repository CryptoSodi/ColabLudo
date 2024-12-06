using LudoClient.Constants;
using LudoClient.ControlView;
using LudoClient.Models;
using System.Text.Json;
namespace LudoClient;
public partial class GamesListPage : ContentPage
{
    public GamesListPage()
    {
        InitializeComponent();
        Tab1.SwitchSource = Tab1.SwitchOn;
        Tab2.SwitchSource = Tab2.SwitchOff;
        InitializeTournamentsAsync();
    }
    public async Task InitializeTournamentsAsync()
    {
        // Retrieve or create a list of tournaments
        List<Game> games = await GetGamesAsync();
        // Dynamically create and add TournamentDetailList controls
        foreach (var game in games)
        {
            var gameDetail = new GameDetailList();
            gameDetail.SetTournamentDetails(game.GameId, game.RoomCode, game.Type, game.BetAmount);
           //// Add the control to the stack layout
            TournamentListStack.Children.Add(gameDetail);
        }
    }
    private async Task<List<Game>> GetGamesAsync()
    {
        HttpResponseMessage response = await GlobalConstants.httpClient.GetAsync("api/Game");
        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var games = JsonSerializer.Deserialize<List<Game>>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return games?.Where(g => g.State == "Active").ToList() ?? new List<Game>();
        }
        else
        {
            // Handle the error case as needed
            return new List<Game>();
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