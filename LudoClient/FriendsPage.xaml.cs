using LudoClient.ControlView;
using SharedCode;
using SharedCode.Constants;
using System.Text.Json;

namespace LudoClient;

public partial class FriendsPage : ContentPage
{
    public FriendsPage()
    {
        InitializeComponent();
        Tab1.SwitchSource = Tab1.SwitchOn;
        Tab2.SwitchSource = Tab2.SwitchOff;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Delay the heavy initialization to allow the page to render first.
        await Task.Delay(100); // Adjust delay as needed
        //GetAllFriendsIncludingPendingRejectedAndGames
        InitializeTournamentsAsync();
    }
    public async Task InitializeTournamentsAsync()
    {

        List<Friends> Friends = await GetGamesAsync(UserInfo.Instance.Id);
        var FriendsIds = Friends.Select(g => g.playerID).ToHashSet();

        // Identify which items are currently displayed
        var existingItems = FriendsListStack.Children.OfType<FriendsDetailList>().ToList();

        var existingGameIds = existingItems.Select(i => i.friend.playerID).ToHashSet();

        // Remove items that are no longer present in the new data
        var itemsToRemove = existingItems.Where(item => !FriendsIds.Contains(item.friend.playerID)).ToList();

        foreach (var item in itemsToRemove)
        {
            FriendsListStack.Children.Remove(item);
        }

        // Add new items that weren't previously displayed
        foreach (var friend in Friends)
        {
            if (!existingGameIds.Contains(friend.playerID))
            {
                var friendDetail = new FriendsDetailList();
                friendDetail.SetFriendsDetail(friend);
                FriendsListStack.Children.Add(friendDetail);
            }
            else
            {
                // Optionally, update existing items if details have changed
                var existingItem = existingItems.FirstOrDefault(i => i.friend.playerID == friend.playerID);
                if (existingItem != null)
                {
                    existingItem.SetFriendsDetail(friend);
                }
            }
        }
    }
    private async Task<List<Friends>> GetGamesAsync(int playerId)
    {
        HttpResponseMessage response = await GlobalConstants.httpClient.GetAsync($"api/Friends?playerId={playerId}");
        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var Friends = JsonSerializer.Deserialize<List<Friends>>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return Friends;
        }
        else
        {
            // Handle the error case as needed
            return new List<Friends>();
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