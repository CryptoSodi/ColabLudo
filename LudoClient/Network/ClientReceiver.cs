using SharedCode;
using SharedCode.Constants;
using LudoClient.Constants;

namespace LudoClient.Network;

public sealed class ClientReceiver
{
    private readonly List<NotificationDTO> _pendingNotifications = new();
    private CancellationTokenSource? _chatPollingCts;
    private Task? _chatPollingTask;
    private CancellationTokenSource? _commandPollingCts;
    private Task? _commandPollingTask;
    public long LastServerTimeMs;
    public long LastLocalReceiveUtcTicks;

    public ClientReceiver()
    {
    }

    public void HandleServerClockPing(long serverTimeMs)
    {
        Interlocked.Exchange(ref LastServerTimeMs, serverTimeMs);
        Interlocked.Exchange(ref LastLocalReceiveUtcTicks, DateTime.UtcNow.Ticks);
    
    }

    public bool IsServerClockPingFresh(int maxAgeMs = 700)
    {
        var receiveTicks = Interlocked.Read(ref LastLocalReceiveUtcTicks);
        if (receiveTicks <= 0)
        {
            Console.WriteLine("[ClockPing] FreshCheck=False Reason=NoSample");
            return false;
        }

        var receiveUtc = new DateTime(receiveTicks, DateTimeKind.Utc);
        var ageMs = (DateTime.UtcNow - receiveUtc).TotalMilliseconds;
        if (ageMs < 0)
        {
            Console.WriteLine($"[ClockPing] FreshCheck=False Reason=NegativeAge AgeMs={ageMs:F1}");
            return false;
        }

        var isFresh = ageMs <= maxAgeMs;
      //  Console.WriteLine($"[ClockPing] FreshCheck={isFresh} AgeMs={ageMs:F1} MaxAgeMs={maxAgeMs} LastServerTimeMs={Interlocked.Read(ref LastServerTimeMs)}");
        return isFresh;
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

    public void StartCommandPolling()
    {
        StopCommandPolling();
        _commandPollingCts = new CancellationTokenSource();
        _commandPollingTask = Task.Run(() => CommandPollingLoopAsync(_commandPollingCts.Token));
    }

    public void StopCommandPolling()
    {
        try
        {
            _commandPollingCts?.Cancel();
        }
        catch
        {
        }
        finally
        {
            _commandPollingCts?.Dispose();
            _commandPollingCts = null;
            _commandPollingTask = null;
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
                if (matchMaker == null || string.IsNullOrWhiteSpace(matchMaker.getAuthToken()))
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

    private async Task CommandPollingLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var game = ClientGlobalConstants.game;
                if (game == null)
                    break;
                if (GlobalConstants.MatchMaker != null && game != null && !string.IsNullOrEmpty(GlobalConstants.RoomCode))
                {
                    int lastSeen = game.engine.EngineHelper.indexServer;
                    List<GameCommand> commands = await GlobalConstants.MatchMaker.PullCommands(lastSeen, GlobalConstants.RoomCode);

                    if (commands?.Count > 0)
                    {
                        foreach (var command in commands.OrderBy(c => c.IndexServer))
                        {
                            game = ClientGlobalConstants.game;
                            if (game == null)
                                break;
                            while (game != null && game.engine.processing)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                await Task.Delay(100, cancellationToken);
                            }

                            bool alreadyHandled = game._commandStore.Any(c => c.IndexServer == command.IndexServer);
                            if (game != null && !alreadyHandled)
                            {
                                await MainThread.InvokeOnMainThreadAsync(async () =>
                                {
                                    while (game.engine.processing || game.isInputLocked) await Task.Delay(10);
                                    try
                                    {
                                        switch (command.SendToClientFunctionName)
                                        {
                                            case "MovePiece":
                                            MovePiece:
                                                if (command.piece1 != null && command.piece2 != null)
                                                {
                                                    Console.WriteLine($"[PullMovePiece] Start IndexServer={command.IndexServer} Piece1={command.piece1} Piece2={command.piece2} LocalIndex={game.engine.EngineHelper.index} LocalIndexServer={game.engine.EngineHelper.indexServer}");
                                                    string result = await game.MovePiece(command.piece1, command.piece2, false);
                                                    Console.WriteLine($"[PullMovePiece] Result={result} IndexServer={command.IndexServer}");
                                                    if (result == "-2")
                                                    {
                                                        Console.WriteLine($"[PullMovePiece] Retry Reason=Busy IndexServer={command.IndexServer}");
                                                        await Task.Delay(100);
                                                        goto MovePiece;
                                                    }
                                                    else if (!result.Contains("-1") && !result.Contains("-0"))
                                                    {
                                                        game._commandStore.Add(command);
                                                        Console.WriteLine($"[PullMovePiece] Stored=True IndexServer={command.IndexServer}");
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine($"[PullMovePiece] Stored=False Reason=InvalidResult IndexServer={command.IndexServer}");
                                                    }
                                                }
                                                break;
                                            case "DiceRoll":
                                            DiceRoll:
                                                if (command.seatName != null && command.diceValue != null && command.piece1 != null && command.piece2 != null)
                                                {
                                                    string result = await game.PlayerDiceClicked(command.seatName, command.diceValue, command.piece1, command.piece2, false);
                                                    if (result == "-2")
                                                    {
                                                        await Task.Delay(100);
                                                        goto DiceRoll;
                                                    }
                                                    else if (!result.Contains("-1") && !result.Contains("-0"))
                                                    {
                                                        game._commandStore.Add(command);
                                                    }
                                                }
                                                break;
                                            case "PlayerLeft":
                                                if (game != null && command.seatName != null)
                                                {
                                                PlayerLeft:
                                                    string result = await game.PlayerLeft(command.seatName, false);
                                                    if (result == "-2")
                                                    {
                                                        await Task.Delay(100);
                                                        goto PlayerLeft;
                                                    }
                                                    else if (!result.Contains("-1") && !result.Contains("-0"))
                                                    {
                                                        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("left");
                                                        game._commandStore.Add(command);
                                                    }
                                                }
                                                break;
                                            case "ShowResults":
                                                Console.WriteLine($"Received ShowResults command. Seats: {command.ShowResultsSeats}, GameType: {command.ShowResultsGameType}, GameCost: {command.ShowResultsGameCost}");
                                                await HandleShowResultsFromCommandAsync(command);
                                                game._commandStore.Add(command);
                                                break;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"ERROR IN SWITCH : 001 {ex.Message}");
                                    }
                                });
                                await Task.Delay(200, cancellationToken);
                            }
                            Console.WriteLine($"Sync states Handled : {alreadyHandled} Index : {game.engine.EngineHelper.index} LoclServerIndex {game.engine.EngineHelper.indexServer} ServerIndex {command.IndexServer}");
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Error pulling commands: EXIT 101");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error pulling commands: {ex.Message}");
            }
            await Task.Delay(1000, cancellationToken);
        }
    }

    private async Task HandleShowResultsFromCommandAsync(GameCommand command)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            StopCommandPolling();

            if (ClientGlobalConstants.game == null)
                return;

            await ClientGlobalConstants.game.ShowResults(
                command.ShowResultsSeats ?? string.Empty,
                command.ShowResultsGameType ?? string.Empty,
                command.ShowResultsGameCost ?? string.Empty);

            ClientGlobalConstants.game.engine.cleanGame();
            ClientGlobalConstants.game = null;
            GlobalConstants.RoomCode = "";
            GlobalConstants.GameCost = 0;
        });
    }

}
