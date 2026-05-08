using SharedCode;
using SharedCode.Constants;

namespace LudoClient.Network;

public sealed class ClientReceiver
{
    private readonly List<NotificationDTO> _pendingNotifications = new();
    private CancellationTokenSource? _chatPollingCts;
    private Task? _chatPollingTask;

    public ClientReceiver()
    {
    }
    public void StartChatPolling()
    {
        StopChatPolling();
        _chatPollingCts = new CancellationTokenSource();
        _chatPollingTask = Task.Run(() => ChatPollingLoopAsync(_chatPollingCts.Token));
    }

    public void StopChatPolling()
    {
        try
        {
            _chatPollingCts?.Cancel();
        }
        catch
        {
        }
        finally
        {
            _chatPollingCts?.Dispose();
            _chatPollingCts = null;
            _chatPollingTask = null;
        }
    }

    public void HandleReceiveChatMessage(List<ChatMessages> messages)
    {
        if (messages == null || messages.Count != 1)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var latestMsg = messages.First();
            if (!string.IsNullOrEmpty(latestMsg.RoomCode))
                return;

            int activeChatId = GetActiveChatPlayerId();
            if (activeChatId == latestMsg.SenderId)
                return;

            if (!string.IsNullOrEmpty(GlobalConstants.RoomCode) || activeChatId != -1)
            {
                lock (_pendingNotifications)
                {
                    var note = new NotificationDTO
                    {
                        Title = latestMsg.SenderName ?? "New Message",
                        Message = latestMsg.Message ?? "",
                        Type = "Message",
                        Payload = latestMsg.SenderId.ToString()
                    };

                    if (!_pendingNotifications.Any(n => n.Payload == note.Payload && n.Message == note.Message))
                        _pendingNotifications.Add(note);
                }

                return;
            }

            ShowNotification(new NotificationDTO
            {
                Title = latestMsg.SenderName ?? "New Message",
                Message = latestMsg.Message ?? "",
                Type = "Message",
                Payload = latestMsg.SenderId.ToString()
            });
        });
    }

    public void HandleReceiveNotification(NotificationDTO notification)
    {
        if (!string.IsNullOrEmpty(GlobalConstants.RoomCode))
        {
            lock (_pendingNotifications)
            {
                if (!_pendingNotifications.Any(n => n.Payload == notification.Payload && n.Message == notification.Message))
                    _pendingNotifications.Add(notification);
            }

            return;
        }

        ShowNotification(notification);
    }

    public async Task CheckForResumeNotificationsAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (string.IsNullOrEmpty(GlobalConstants.RoomCode))
            {
                int activeChatId = GetActiveChatPlayerId();
                if (activeChatId != -1)
                {
                    lock (_pendingNotifications)
                    {
                        _pendingNotifications.RemoveAll(n => n.Type == "Message" && n.Payload == activeChatId.ToString());
                    }
                }

                if (_pendingNotifications.Count > 0)
                    await ProcessPendingNotificationsAsync();
            }

            await Task.Delay(2000, token);
        }
    }

    private async Task ProcessPendingNotificationsAsync()
    {
        List<NotificationDTO> toProcess;
        lock (_pendingNotifications)
        {
            int activeChatId = GetActiveChatPlayerId();
            toProcess = _pendingNotifications
                .Where(n => !(n.Type == "Message" && n.Payload == activeChatId.ToString()))
                .ToList();

            _pendingNotifications.Clear();
        }

        foreach (var note in toProcess)
        {
            ShowNotification(note);
            await Task.Delay(1500);
        }
    }
    private void ShowNotification(NotificationDTO notification)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var snackbarOptions = new CommunityToolkit.Maui.Core.SnackbarOptions
            {
                BackgroundColor = Color.FromArgb("#CC143450"), // Semi-transparent Ludo Blue
                TextColor = Colors.White,
                ActionButtonTextColor = Colors.Yellow,
                CornerRadius = new CornerRadius(10),
                Font = Microsoft.Maui.Font.SystemFontOfSize(14),
                ActionButtonFont = Microsoft.Maui.Font.SystemFontOfSize(14, Microsoft.Maui.FontWeight.Bold)
            };

            var snackbar = CommunityToolkit.Maui.Alerts.Snackbar.Make(
                $"{notification.Title}\n{notification.Message}",
                async () => {
                    try
                    {
                        if (notification.Type == "Message")
                        {
                            int senderId = int.Parse(notification.Payload);
                            var playerCard = await GlobalConstants.MatchMaker.GetPlayerById(senderId);
                            if (playerCard != null)
                            {
                                // Robust navigation using Shell
                                await MainThread.InvokeOnMainThreadAsync(async () => {
                                    await Shell.Current.Navigation.PushAsync(new ChatPage(playerCard));
                                });
                            }
                        }
                        else if (notification.Type == "TournamentResults")
                        {
                            await MainThread.InvokeOnMainThreadAsync(async () => {
                                await Shell.Current.GoToAsync("//LeaderboardPage");
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error navigating from notification: {ex.Message}");
                    }
                }, "OPEN", TimeSpan.FromSeconds(5), snackbarOptions);

            await snackbar.Show();
        });
    }

    private int GetActiveChatPlayerId()
    {
        try
        {
            if (Application.Current?.MainPage is AppShell shell)
            {
                var stack = shell.Navigation.NavigationStack;
                if (stack.Count > 0 && stack.Last() is ChatPage cp)
                    return cp.playerCard.playerID;
            }
        }
        catch
        {
        }

        return -1;
    }

    private async Task ChatPollingLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var matchMaker = GlobalConstants.MatchMaker;
                if (matchMaker == null || !matchMaker.Connected || string.IsNullOrWhiteSpace(matchMaker.getAuthToken()))
                {
                    await Task.Delay(1500, token);
                    continue;
                }

                await matchMaker.PollChatOnceAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClientReceiver] ChatPollingLoop Error: {ex.Message}");
            }

            await Task.Delay(1500, token);
        }
    }
}
