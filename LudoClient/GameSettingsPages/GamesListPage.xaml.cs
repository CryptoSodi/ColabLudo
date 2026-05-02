using LudoClient.Constants;
using LudoClient.ControlView;
using SharedCode;
using SharedCode.Constants;
namespace LudoClient;
public partial class GamesListPage : ContentPage
{ 
    bool _isRunning = true;

    public GamesListPage()
    {
        InitializeComponent();
        Tab1.SwitchSource = Tab1.SwitchOn;
        Tab2.SwitchSource = Tab2.SwitchOff;
        // _ = InitializeTournamentsAsync();
    }

    [Obsolete]
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Initial load when page appears
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            _ = InitializeTournamentsAsync();
        });
        _isRunning = true;
        Device.StartTimer(TimeSpan.FromSeconds(10), () =>
        {
            // Run the async method on the main thread
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await InitializeTournamentsAsync();
            });
            return _isRunning; // Return true to repeat, false to stop
        });
    }
    public async Task InitializeTournamentsAsync()
    {
        var newGames = await GetGamesAsync();
        var newGameIds = newGames.Select(g => g.GameId).ToHashSet();

        // Identify which items are currently displayed
        var existingItems = TournamentListStack.Children.OfType<GameDetailList>().ToList();
        var existingGameIds = existingItems.Select(i => i.gameId).ToHashSet();

        // Remove items that are no longer present in the new data
        var itemsToRemove = existingItems.Where(item => !newGameIds.Contains(item.gameId)).ToList();
        foreach (var item in itemsToRemove)
        {
            TournamentListStack.Children.Remove(item);
        }

        // Add new items that weren't previously displayed
        foreach (var game in newGames)
        {
            if (!existingGameIds.Contains(game.GameId))
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var gameDetail = new GameDetailList();
                    gameDetail.SetTournamentDetails(game.GameId, game.RoomCode, game.GameType, game.BetAmount);
                    TournamentListStack.Children.Add(gameDetail);
                });
            }
            else
            {
                // Optionally, update existing items if details have changed
                var existingItem = existingItems.FirstOrDefault(i => i.gameId == game.GameId);
                if (existingItem != null)
                {
                    existingItem.SetTournamentDetails(game.GameId, game.RoomCode, game.GameType, game.BetAmount);
                }
            }
        }
    }
    private async Task<List<ActiveGameListItem>> GetGamesAsync()
    {
        List<ActiveGameListItem> games = await GlobalConstants.MatchMaker.GetActivePublicGamesAsync().ConfigureAwait(false);
        return games?.Where(g => g.State == "Active").ToList() ?? new List<ActiveGameListItem>();
    }
    private void TabRequestedActivate(object sender, EventArgs e)
    {
        if (sender is ImageSwitch activeTab)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            Tab1.SwitchSource = Tab1 == activeTab ? Tab1.SwitchOn : Tab1.SwitchOff;
            Tab2.SwitchSource = Tab2 == activeTab ? Tab2.SwitchOn : Tab2.SwitchOff;
        }
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isRunning = false; // Stop the timer if the page is not visible
    }
}
