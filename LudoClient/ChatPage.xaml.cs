using LudoClient.Constants;
using LudoClient.ControlView;
using SharedCode;
using SharedCode.Constants;

namespace LudoClient;

public partial class ChatPage : ContentPage
{
    PlayerCard playerCard;
    string roomCode;
    private int _messagesToLoad = 10;
    private List<ChatMessages> _allMessages = new();

    public ChatPage(PlayerCard playerCard, String RoomCode = "")
    {
        this.playerCard = playerCard;
        this.roomCode = RoomCode;

        InitializeComponent();
        Header.SetDetails(playerCard, "Header");

        GlobalConstants.MatchMaker.ReceiveChatMessage += UpdateMessages;
    }

    private void ChatScrollView_Scrolled(object sender, ScrolledEventArgs e)
    {
        if (e.ScrollY <= 0 && _allMessages.Count > 0 && _messagesToLoad < _allMessages.Count)
        {
            _messagesToLoad += 10;
            RenderMessages();
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Fetch history after appearing to ensure connection and listeners are ready
        ChatMessages cm = new();
        cm.SenderId = UserInfo.Instance.player.PlayerId;
        cm.SenderName = UserInfo.Instance.player.Name;
        cm.SenderPicture = UserInfo.Instance.player.PictureUrl;
        cm.ReceiverId = playerCard.playerID;
        cm.ReceiverName = playerCard.name;
        cm.Message = "";
        cm.RoomCode = this.roomCode;
        cm.CreatedDate = DateTime.UtcNow;

        GlobalConstants.MatchMaker?.SendChatMessageAsync(cm, this.roomCode).ContinueWith(t =>
        {
            if (t.Status == TaskStatus.RanToCompletion)
            {
                UpdateMessages(this, t.Result);
            }
        });

        await Task.Delay(200);
        await ChatScrollView.ScrollToAsync(0, 40000, true);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        GlobalConstants.MatchMaker.ReceiveChatMessage -= UpdateMessages;
    }

    public void UpdateMessages(object sender, List<ChatMessages> messages)
    {
        if (messages == null || messages.Count == 0) return;

        lock (_allMessages)
        {
            int maxExistingIndex = _allMessages.Any() ? _allMessages.Max(m => m.Index) : -1;
            bool hasNewMessages = false;

            foreach (var msg in messages)
            {
                if (!_allMessages.Any(m => m.Index == msg.Index))
                {
                    _allMessages.Add(msg);
                    if (msg.Index > maxExistingIndex)
                    {
                        hasNewMessages = true;
                    }
                }
            }
            
            _allMessages = _allMessages.OrderBy(m => m.Index).ToList();

            // If new messages arrived (not just history), increment the load limit
            if (hasNewMessages)
            {
                _messagesToLoad++;
            }
        }
        
        RenderMessages();
    }

    private void RenderMessages()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var existingIndices = MessagesListStack.Children.OfType<ChatCard>()
                    .Select(cc => cc.Message?.Index)
                    .Where(idx => idx.HasValue)
                    .ToHashSet();

                var visibleMessages = _allMessages.TakeLast(_messagesToLoad).ToList();
                bool addedAtEnd = false;

                foreach (ChatMessages cm in visibleMessages)
                {
                    if (!existingIndices.Contains(cm.Index))
                    {
                        ChatCard cc = new();
                        if (UserInfo.Instance.player.PlayerId == cm.SenderId)
                            cc.SetDetails(cm, "Right", "yellow");
                        else
                            cc.SetDetails(cm, "Left", "white");

                        if (!existingIndices.Any() || cm.Index > existingIndices.Max())
                        {
                            MessagesListStack.Children.Add(cc);
                            addedAtEnd = true;
                        }
                        else
                        {
                            MessagesListStack.Children.Insert(0, cc);
                        }
                    }
                }

                if (addedAtEnd)
                {
                    await Task.Delay(50);
                    await ChatScrollView.ScrollToAsync(0, 40000, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rendering chat messages: {ex.Message}");
            }
        });
    }

    private void MessageEntry_Completed(object sender, EventArgs e)
    {
        MessageEntry.Unfocus();
        OnSendButton_Tapped(null, null);
    }

    protected override bool OnBackButtonPressed()
    {
        OnBackButton_Tapped(null, null);
        return true;
    }

    private async void OnBackButton_Tapped(object sender, TappedEventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        HideKeyboard();
        await Task.Delay(100);
        await Navigation.PopAsync();
    }

    public void HideKeyboard()
    {
        MessageEntry.Unfocus();
#if ANDROID
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        var inputMethodManager = activity?.GetSystemService(global::Android.Content.Context.InputMethodService)
                                as global::Android.Views.InputMethods.InputMethodManager;

        if (activity?.Window?.DecorView?.WindowToken != null)
        {
            inputMethodManager?.HideSoftInputFromWindow(activity.Window.DecorView.WindowToken, global::Android.Views.InputMethods.HideSoftInputFlags.None);
        }
#endif
    }

    private void OnSendButton_Tapped(object sender, TappedEventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        if (!string.IsNullOrEmpty(MessageEntry.Text))
        {
            ChatMessages cm = new();
            cm.SenderId = UserInfo.Instance.player.PlayerId;
            cm.SenderName = UserInfo.Instance.player.Name;
            cm.SenderPicture = UserInfo.Instance.player.PictureUrl;
            cm.ReceiverId = playerCard.playerID;
            cm.ReceiverName = playerCard.name;
            cm.SenderColor = "";
            cm.Message = MessageEntry.Text;
            cm.RoomCode = this.roomCode;
            cm.CreatedDate = DateTime.UtcNow;
            MessageEntry.Text = "";

            GlobalConstants.MatchMaker?.SendChatMessageAsync(cm, this.roomCode).ContinueWith(t =>
            {
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    UpdateMessages(this, t.Result);
                }
            });
        }
    }
}
