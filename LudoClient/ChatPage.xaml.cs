
using LudoClient.Constants;
using LudoClient.ControlView;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Maui.Controls;
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
        int myPlayerId = UserInfo.Instance.Id;
        GlobalConstants.MatchMaker._hubConnection.InvokeAsync("UserConnectedSetID", myPlayerId);

        ChatMessages cm = new();
        cm.SenderId = UserInfo.Instance.Id;
        cm.SenderName = UserInfo.Instance.Name;
        cm.SenderPicture = UserInfo.Instance.PictureUrl;
        cm.ReceiverId = playerCard.playerID;
        cm.ReceiverName = playerCard.playerName;
        cm.Message = "";
        cm.Time = DateTime.Now;

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

    public void SetDetails(PlayerCard playerCard, List<ChatMessages> messages)
    {
        foreach(ChatMessages cm in messages)
        {
            ChatCard cc = new();
            
            if (UserInfo.Instance.Id == cm.SenderId)
                cc.SetDetails(cm, "Right", "blue");
            else
                cc.SetDetails(cm, "Left", "white");

            MessagesListStack.Children.Add(cc);
        }
        Header.SetDetails(playerCard, "Header");
    }
    private void MessageEntry_Completed(object sender, EventArgs e)
    {
        MessageEntry.Unfocus();
        OnSendButton_Tapped(null, null);
    }
    private void OnBackButton_Tapped(object sender, TappedEventArgs e)
    {
        Navigation.PopAsync();
    }
    private void OnSendButton_Tapped(object sender, TappedEventArgs e)
    {
        if (MessageEntry.Text != "")
        {
            ChatMessages cm = new();
            cm.SenderId = UserInfo.Instance.Id;
            cm.SenderName = UserInfo.Instance.Name;
            cm.SenderPicture = UserInfo.Instance.PictureUrl;
            cm.ReceiverId = playerCard.playerID;
            cm.ReceiverName = playerCard.playerName;
            cm.SenderColor = "";
            //cm.ReceiverPicture = playerCard.playerPicture;
            cm.Message = MessageEntry.Text;
            cm.Time = DateTime.Now;
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
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Get the existing ChatMessages directly from ChatCard
            var existingMessages = MessagesListStack.Children.OfType<ChatCard>()
                .Select(cc => cc.Message)
                .Where(cm => cm != null)
                .ToHashSet();

            foreach (ChatMessages cm in messages)
            { // Check if the message is already present based on SenderId, ReceiverId, Message, and Time
                bool isAlreadyPresent = existingMessages.Any(existing => existing.Index == cm.Index);

                if (!isAlreadyPresent)
                {
                    ChatCard cc = new();
                    MessagesListStack.Children.Add(cc);

                    if (UserInfo.Instance.Id == cm.SenderId)
                        cc.SetDetails(cm, "Right", "yellow");
                    else
                        cc.SetDetails(cm, "Left", "white");
                    // Optional: scroll to bottom

                }
            }
            // After adding your chat cards inside MainThread.BeginInvokeOnMainThread:
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // Force layout to update ContentSize
                await Task.Delay(100);
                // Scroll to the bottom-most Y coordinate
                //double bottomY = ChatScrollView.ContentSize.Height;
                await ChatScrollView.ScrollToAsync(0, 40000, true);
            });
        });
    }
}