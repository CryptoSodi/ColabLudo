using LudoClient.Constants;
using LudoClient.ControlView;
using Microsoft.AspNetCore.SignalR.Client;
using SharedCode;
using SharedCode.Constants;

namespace LudoClient;

public partial class FriendsPage : ContentPage
{
    String Filter = "Normal";
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
        InitializeFriendsAsync();
    }
    public async Task InitializeFriendsAsync()
    {
        List<PlayerCard> playerCard = await GetPlayerCards();
        var FriendsIds = playerCard.Select(g => g.playerID).ToHashSet();

        // Identify which items are currently displayed
        var existingItems = FriendsListStack.Children.OfType<DetailList>().ToList();

        var existingFriendsIds = existingItems.Select(i => i.playerCard.playerID).ToHashSet();

        // Remove items that are no longer present in the new data
        var itemsToRemove = existingItems.Where(item => !FriendsIds.Contains(item.playerCard.playerID)).ToList();
        
        foreach (var item in itemsToRemove)
        {
            FriendsListStack.Children.Remove(item);
        }

        // Add new items that weren't previously displayed
        foreach (var PI in playerCard)
        {
            if (!existingFriendsIds.Contains(PI.playerID))
            {
                var friendDetail = new DetailList(PI, "Friend");                
                FriendsListStack.Children.Add(friendDetail);
            }
            else
            {
                // Optionally, update existing items if details have changed
                var existingItem = existingItems.FirstOrDefault(i => i.playerCard.playerID == PI.playerID);
                if (existingItem != null)
                {
                    existingItem.SetDetails(PI, "Friend");
                }
            }
        }
    }
    private async Task<List<PlayerCard>> GetPlayerCards()
    {
        List<PlayerCard> Friends = await GlobalConstants.MatchMaker._hubConnection.InvokeAsync<List<PlayerCard>>("GetFriends", "All").ConfigureAwait(false);
        if (Filter == "BLOCK") // Remove friends where status is "Block"
            Friends = Friends.Where(f => f.status == "BLOCK").ToList();
        else
            Friends = Friends.Where(f => f.status != "BLOCK").ToList();
        return Friends;
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
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        Tab1.SwitchSource = Tab1 == activeTab ? Tab1.SwitchOn : Tab1.SwitchOff;
        Tab2.SwitchSource = Tab2 == activeTab ? Tab2.SwitchOn : Tab2.SwitchOff;
        // Add logic here to change the content based on the active tab
        if(Tab2 == activeTab)
            Filter = "BLOCK";
        else
            Filter = "NORMAL";
        InitializeFriendsAsync();
    }
}