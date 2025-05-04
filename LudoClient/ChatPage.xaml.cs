
using LudoClient.ControlView;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Maui.Controls;
using SharedCode;
using SharedCode.Constants;
using System.Collections.Generic;

namespace LudoClient;

public partial class ChatPage : ContentPage
{
    PlayerCard playerCard;
    public ChatPage(PlayerCard playerCard)
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

        int myPlayerId = UserInfo.Instance.Id;
        GlobalConstants.MatchMaker._hubConnection.InvokeAsync("UserConnectedSetID", myPlayerId);

        GlobalConstants.MatchMaker.ReceiveChatMessage += UpdateMessages;
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
        // … your send logic …

        // Dismiss keyboard:
        MessageEntry.Unfocus();
        OnSendButton_Tapped(null, null);
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
            cm.ReceiverPicture = playerCard.playerPicture;
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
            foreach (ChatMessages cm in messages)
            {
                ChatCard cc = new();
                MessagesListStack.Children.Add(cc);

                if (UserInfo.Instance.Id == cm.SenderId)
                    cc.SetDetails(cm, "Right", "yellow");
                else
                    cc.SetDetails(cm, "Left", "white");
                // Optional: scroll to bottom

                // After adding your chat cards inside MainThread.BeginInvokeOnMainThread:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    // Force layout to update ContentSize
                    await Task.Delay(50);

                    // Scroll to the bottom-most Y coordinate
                    double bottomY = ChatScrollView.ContentSize.Height;
                    await ChatScrollView.ScrollToAsync(0, bottomY, true);
                });
            }
            {
                Console.WriteLine("ERROR SERVER OUT OF SYNC AT PIECE");
            }
        });
    }
}