using LudoClient.Constants;
using LudoClient.ControlView;
using Microsoft.AspNetCore.SignalR.Client;
using SharedCode;
using SharedCode.Constants;

namespace LudoClient;

public partial class LeaderboardPage : ContentPage
{
    String Filter = "Normal";
    public LeaderboardPage()
    {
        InitializeComponent();
        Tab1.SwitchSource = Tab1.SwitchOn;
        Tab2.SwitchSource = Tab2.SwitchOff;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        InitializeLeaderboardAsync();
    }
    public async Task InitializeLeaderboardAsync()
    {
        List<PlayerCard> Friends = await GetPlayerCards(UserInfo.Instance.player.PlayerId);
        var FriendsIds = Friends.Select(g => g.playerID).ToHashSet();

        // Identify which items are currently displayed
        var existingItems = LeaderboardListStack.Children.OfType<DetailList>().ToList();

        var existingFriendsIds = existingItems.Select(i => i.playerCard.playerID).ToHashSet();

        // Remove items that are no longer present in the new data
        var itemsToRemove = existingItems.Where(item => !FriendsIds.Contains(item.playerCard.playerID)).ToList();

        foreach (var item in itemsToRemove)
        {
            LeaderboardListStack.Children.Remove(item);
        }

        // Add new items that weren't previously displayed
        foreach (var friend in Friends)
        {
            if (!existingFriendsIds.Contains(friend.playerID))
            {
                var friendDetail = new DetailList(friend, "Leaderboard");
                LeaderboardListStack.Children.Add(friendDetail);
            }
            else
            {
                // Optionally, update existing items if details have changed
                var existingItem = existingItems.FirstOrDefault(i => i.playerCard.playerID == friend.playerID);
                if (existingItem != null)
                {
                    existingItem.SetDetails(friend, "Leaderboard");
                }
            }
        }
    }
    private async Task<List<PlayerCard>> GetPlayerCards(int playerId)
    {
        List<PlayerCard> Friends = await GlobalConstants.MatchMaker._hubConnection.InvokeAsync<List<PlayerCard>>("GetFriends").ConfigureAwait(false);
        if (Filter == "ADD FRIEND")
        {
            Friends = Friends.Where(f => f.status == "ADD FRIEND").ToList();
        }
        return Friends;
    }
    private void TabRequestedActivate(object sender, EventArgs e)
    {
        ActivateTab(sender as ImageSwitch);
    }
    private void ActivateTab(ImageSwitch activeTab)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        Tab1.SwitchSource = Tab1 == activeTab ? Tab1.SwitchOn : Tab1.SwitchOff;
        Tab2.SwitchSource = Tab2 == activeTab ? Tab2.SwitchOn : Tab2.SwitchOff;
        // Add logic here to change the content based on the active tab
        if (Tab2 == activeTab)
            Filter = "ADD FRIEND";
        else
            Filter = "";
        InitializeLeaderboardAsync();
    }
}