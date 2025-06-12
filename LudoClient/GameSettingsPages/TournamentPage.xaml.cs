using LudoClient.Constants;
using LudoClient.ControlView;
using LudoClient.Models;
using SharedCode;
using SharedCode.Constants;
using System.Collections.Generic;
using System.Text.Json;
namespace LudoClient;
public partial class TournamentPage : ContentPage
{
    public TournamentPage()
    {
        InitializeComponent();
        Tab1.SwitchSource = Tab1.SwitchOn;
        Tab2.SwitchSource = Tab2.SwitchOff;
        Tab3.SwitchSource = Tab3.SwitchOff;
        Tab4.SwitchSource = Tab4.SwitchOff;

        InitializeTournamentsAsync("Local");
    }
    public async Task InitializeTournamentsAsync(String TabType)
    {
        // 1) Clear old items
        TournamentListStack.Children.Clear();

        // 2) Fetch all tournaments from the API        

        List<TournamentDTO> tournaments = await GlobalConstants.MatchMaker.GetAllTournaments("All");

        // 3) If "Local" tab is requested, filter by user's city
        if (TabType == "Local")
        {
            var userCity = UserInfo.Instance.City?.Trim() ?? string.Empty;

            tournaments = tournaments.Where(t =>string.Equals(t.City?.Trim(), userCity, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        if (TabType == "Global")
        {
            //tournaments = tournaments
            //    .Where(t =>
            //        string.Equals(t.City?.Trim(), "Global", StringComparison.OrdinalIgnoreCase)
            //    )
            //    .ToList();
        }
        if (TabType == "Ended")
        {
            // Only keep tournaments whose EndDate has already passed
            // (assuming EndDate and ServerDateTime are both DateTime types)
            tournaments = tournaments
                .Where(t => t.EndDate <= t.ServerDateTime)
                .ToList();
        }
        if (TabType == "Active")
        {
            // Only keep tournaments that are currently running:
            // ServerDateTime is between StartDate and EndDate (inclusive)
            tournaments = tournaments
                .Where(t => t.ServerDateTime >= t.StartDate &&
                            t.ServerDateTime <= t.EndDate)
                .ToList();
        }
        // 4) Populate the UI with whatever is left
        foreach (var tournament in tournaments)
        {
            var tournamentDetail = new TournamentDetailList(tournament);
            TournamentListStack.Children.Add(tournamentDetail);
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
            Tab4.SwitchSource = Tab4 == activeTab ? Tab4.SwitchOn : Tab4.SwitchOff;
            // Add logic here to change the content based on the active tab
            // 1) Note which tab is active (for example, store an index)
            if (activeTab == Tab1)
                InitializeTournamentsAsync("Local");
            else if (activeTab == Tab2)
                InitializeTournamentsAsync("Global");
            else if (activeTab == Tab3)
                InitializeTournamentsAsync("Active");
            else // activeTab == Tab3
                InitializeTournamentsAsync("Ended");
        }
    }
}