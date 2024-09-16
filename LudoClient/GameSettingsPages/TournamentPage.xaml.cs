using LudoClient.ControlView;
using LudoClient.Models;

namespace LudoClient;

public partial class TournamentPage : ContentPage
{
    public TournamentPage()
    {
        InitializeComponent();
        Tab1.SwitchSource = Tab1.SwitchOn;
        Tab2.SwitchSource = Tab2.SwitchOff;

        // Retrieve or create a list of tournaments
        var tournaments = GetTournaments();

        // Dynamically create and add TournamentDetailList controls
        foreach (var tournament in tournaments)
        {
            var tournamentDetail = new TournamentDetailList();
            tournamentDetail.SetTournamentDetails(
                tournamentName: tournament.TournamentName,
                startDate: tournament.StartDate.ToString("g"),
                endDate: tournament.EndDate.ToString("g"),
                joiningFee: tournament.EntryPrice,
                prizeAmount: tournament.PrizeAmount
            );

            // Add the control to the stack layout
            TournamentListStack.Children.Add(tournamentDetail);
        }
    }
    /// <summary>
    /// Retrieves a list of tournaments. Replace this with your actual data source.
    /// </summary>
    /// <returns>List of tournaments</returns>
    private List<Tournament> GetTournaments()
    {
        // Sample data. Replace with actual data retrieval logic.
        return new List<Tournament>
            {
                new Tournament
                {
                    TournamentName = "Monthly Tournament",
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(1),
                    EntryPrice = 100,
                    PrizeAmount = 1000
                },
                new Tournament
                {
                    TournamentName = "Weekly Championship",
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(7),
                    EntryPrice = 50,
                    PrizeAmount = 500
                },
                // Add more tournaments as needed
            };
    }
    private void TabRequestedActivate(object sender, EventArgs e)
    {
        ActivateTab(sender as ImageSwitch);
    }
    private void ActivateTab(ImageSwitch activeTab)
    {
        Tab1.SwitchSource = Tab1 == activeTab ? Tab1.SwitchOn : Tab1.SwitchOff;
        Tab2.SwitchSource = Tab2 == activeTab ? Tab2.SwitchOn : Tab2.SwitchOff;
        // Add logic here to change the content based on the active tab
    }
}