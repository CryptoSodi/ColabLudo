
using LudoClient.Constants;
using LudoClient.ControlView;
using SharedCode;
using SharedCode.Constants;

namespace LudoClient;

public partial class ChatPage : ContentPage
{
    PlayerCard playerCard;
    public ChatPage(PlayerCard playerCard, String RoomCode="")
	{
        this.playerCard = playerCard;

        InitializeComponent();
        //PlayerCard playerCard = new PlayerCard();
        //playerCard.playerName = "Sodi";
        //playerCard.rank = 0;
        //playerCard.status = "AddFriend";
        //playerCard.playerPicture = "https://yt3.ggpht.com/ytc/AIdro_nuNlfceTDiBSTQUhxQ56YDJFbBu1DjRfTpJMFP6ck9D0x3tsglom8eMUA2blBLpRVU8w=s108-c-k-c0x00ffffff-no-rj";
        
        //playerCard.status = "AddFriend";
        //playerCard.status = "Friend";
        SetDetails(playerCard, new List<ChatMessages>());

        GlobalConstants.MatchMaker.ReceiveChatMessage += UpdateMessages;        

        ChatMessages cm = new();
        cm.SenderId = UserInfo.Instance.player.PlayerId;
        cm.SenderName = UserInfo.Instance.player.Name;
        cm.SenderPicture = UserInfo.Instance.player.PictureUrl;
        cm.ReceiverId = playerCard.playerID;
        cm.ReceiverName = playerCard.name;
        cm.Message = "";
        cm.CreatedDate = DateTime.UtcNow;

        GlobalConstants.MatchMaker?.SendChatMessageAsync(cm, GlobalConstants.RoomCode).ContinueWith(t =>
        {
            if (t.Status == TaskStatus.RanToCompletion)
            {
                List<ChatMessages> messages = t.Result;
                UpdateMessages(this, (messages));
            }
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Force layout to update ContentSize
        await Task.Delay(100);
        await ChatScrollView.ScrollToAsync(0, 40000, true);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        GlobalConstants.MatchMaker.ReceiveChatMessage -= UpdateMessages;
    }

    public void SetDetails(PlayerCard playerCard, List<ChatMessages> messages)
    {
        UpdateMessages(this, messages);
        Header.SetDetails(playerCard, "Header");
    }
    private void MessageEntry_Completed(object sender, EventArgs e)
    {
        MessageEntry.Unfocus();
        OnSendButton_Tapped(null, null);
    }
    private void OnBackButton_Tapped(object sender, TappedEventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        Navigation.PopAsync();
    }
    private void OnSendButton_Tapped(object sender, TappedEventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        if (MessageEntry.Text != "")
        {
            ChatMessages cm = new();
            cm.SenderId = UserInfo.Instance.player.PlayerId;
            cm.SenderName = UserInfo.Instance.player.Name;
            cm.SenderPicture = UserInfo.Instance.player.PictureUrl;
            cm.ReceiverId = playerCard.playerID;
            cm.ReceiverName = playerCard.name;
            cm.SenderColor = "";
            //cm.ReceiverPicture = playerCard.playerPicture;
            cm.Message = MessageEntry.Text;
            cm.CreatedDate = DateTime.UtcNow;
            MessageEntry.Text = "";

            GlobalConstants.MatchMaker?.SendChatMessageAsync(cm, GlobalConstants.RoomCode).ContinueWith(t =>
            {
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    List<ChatMessages> messages = t.Result;
                    UpdateMessages(this, (messages));
                }
                else
                {
                    //ServerpieceName = "Error"; // Handle failure
                }
            });
        }
    }
    public void UpdateMessages(object sender, List<ChatMessages> messages)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                // Get the existing ChatMessages indices to prevent duplicates
                var existingIndices = MessagesListStack.Children.OfType<ChatCard>()
                    .Select(cc => cc.Message?.Index)
                    .Where(idx => idx.HasValue)
                    .ToHashSet();

                bool added = false;
                foreach (ChatMessages cm in messages)
                { 
                    if (!existingIndices.Contains(cm.Index))
                    {
                        ChatCard cc = new();
                        if (UserInfo.Instance.player.PlayerId == cm.SenderId)
                            cc.SetDetails(cm, "Right", "yellow");
                        else
                            cc.SetDetails(cm, "Left", "white");

                        MessagesListStack.Children.Add(cc);
                        added = true;
                    }
                }

                if (added)
                {
                    // Force layout to update ContentSize
                    await Task.Delay(100);
                    await ChatScrollView.ScrollToAsync(0, 40000, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating chat messages: {ex.Message}");
            }
        });
    }
}