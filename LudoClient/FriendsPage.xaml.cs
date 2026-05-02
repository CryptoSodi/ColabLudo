using LudoClient.Constants;
using LudoClient.ControlView;
using SharedCode;
using SharedCode.Constants;
#if ANDROID
using Android.Views;
using LudoClient.Platforms.Android;
#endif

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
        var existingItems = FriendsListStack.Children.ToList();

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
            FriendsListStack.Children.Remove(item);
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
                var nativeCard = new NativeFriendCard(PI, "Friend");
                FriendsListStack.Children.Add(nativeCard);
#else
                var friendDetail = new DetailList(PI, "Friend");                
                FriendsListStack.Children.Add(friendDetail);
#endif
            }
            else
            {
                if (existingItem is DetailList dl)
                {
                    dl.SetDetails(PI, "Friend");
                }
#if ANDROID
                else if (existingItem is NativeFriendCard nfc)
                {
                    nfc.Player = PI;
                    // Trigger a re-map by setting the property (optional if same instance)
                    // nfc.OnPropertyChanged(nameof(nfc.Player)); 
                }
#endif
            }
        }
    }
    private async Task<List<PlayerCard>> GetPlayerCards()
    {
        List<PlayerCard> Friends = await GlobalConstants.MatchMaker.GetFriends("All");
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
