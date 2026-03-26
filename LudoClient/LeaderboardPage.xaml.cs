using LudoClient.Constants;
using LudoClient.ControlView;
using Microsoft.AspNetCore.SignalR.Client;
using SharedCode;
using SharedCode.Constants;
#if ANDROID
using LudoClient.Platforms.Android;
#endif

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
        await Task.Delay(100);
        InitializeLeaderboardAsync();
    }
    public async Task InitializeLeaderboardAsync()
    {
        try
        {
            List<PlayerCard> playerCard = await GetPlayerCards(UserInfo.Instance.player.PlayerId);
            
            if (playerCard == null)
            {
                Console.WriteLine("GetPlayerCards returned null.");
                return;
            }

            var FriendsIds = playerCard.Select(g => g.playerID).ToHashSet();

            // Identify which items are currently displayed
            var existingItems = LeaderboardListStack.Children.ToList();

            // Remove items that are no longer present in the new data
            var itemsToRemove = existingItems.Where(item => {
                if (item is DetailList dl) return !FriendsIds.Contains(dl.playerCard.playerID);
#if ANDROID
                if (item is NativeFriendCard nfc) return !FriendsIds.Contains(nfc.Player.playerID);
#endif
                return true;
            }).ToList();

            foreach (var item in itemsToRemove)
            {
                LeaderboardListStack.Children.Remove(item);
            }

            // Add or update items
            foreach (var PI in playerCard)
            {
                var existingItem = existingItems.FirstOrDefault(item => {
                    if (item is DetailList dl) return dl.playerCard.playerID == PI.playerID;
#if ANDROID
                    if (item is NativeFriendCard nfc) return nfc.Player.playerID == PI.playerID;
#endif
                    return false;
                });

                if (existingItem == null)
                {
#if ANDROID
                    var nativeCard = new NativeFriendCard(PI, "Leaderboard");
                    LeaderboardListStack.Children.Add(nativeCard);
#else
                    var friendDetail = new DetailList(PI, "Leaderboard");
                    LeaderboardListStack.Children.Add(friendDetail);
#endif
                }
                else
                {
                    if (existingItem is DetailList dl)
                    {
                        dl.SetDetails(PI, "Leaderboard");
                    }
#if ANDROID
                    else if (existingItem is NativeFriendCard nfc)
                    {
                        nfc.Player = PI;
                    }
#endif
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing leaderboard: {ex.Message}");
        }
    }
    private async Task<List<PlayerCard>> GetPlayerCards(int playerId)
    {
        List<PlayerCard> Friends = await GlobalConstants.MatchMaker._hubConnection.InvokeAsync<List<PlayerCard>>("GetFriends", "All").ConfigureAwait(false);
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