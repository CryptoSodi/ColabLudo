using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using LudoClient.Constants;
using LudoClient.ControlView;
using SharedCode.Constants;
#if ANDROID
using LudoClient.Platforms.Android;
#endif

namespace LudoClient;

public enum WalletSectionKind
{
    Bonus,
    Deposit,
    Withdraw,
    Games
}

public partial class WalletSectionPage : ContentPage
{
    private readonly WalletSectionKind _section;
    private IReadOnlyList<WalletBonusHistoryItem> _bonusItems = Array.Empty<WalletBonusHistoryItem>();
    private IReadOnlyList<WalletDepositHistoryItem> _depositItems = Array.Empty<WalletDepositHistoryItem>();
    private IReadOnlyList<WalletWithdrawalHistoryItem> _withdrawalItems = Array.Empty<WalletWithdrawalHistoryItem>();
    private IReadOnlyList<WalletGameHistoryItem> _gameItems = Array.Empty<WalletGameHistoryItem>();
    private ImageSwitch? _activeTab;

    public WalletSectionPage(WalletSectionKind section)
    {
        InitializeComponent();
        _section = section;
        ConfigureShell();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_activeTab == null)
        {
            ActivateTab(Tab1);
            await LoadAsync();
        }
    }

    protected override void OnDisappearing()
    {
        ReleaseCurrentRows();
        base.OnDisappearing();
    }

    private void ConfigureShell()
    {
        switch (_section)
        {
            case WalletSectionKind.Bonus:
                SetTitle("BONUS");
                HeaderLabel1.Text = "TYPE";
                HeaderLabel2.Text = "STATUS";
                HeaderLabel3.Text = "AMOUNT";
                ConfigureTabs("ALL", "DAILY", "REFER", "AIRDROP");
                break;

            case WalletSectionKind.Deposit:
                SetTitle("DEPOSIT");
                HeaderLabel1.Text = "SOURCE";
                HeaderLabel2.Text = "STATUS";
                HeaderLabel3.Text = "AMOUNT";
                ConfigureTabs("ALL", "DIRECT", "LOCAL", "MANUAL");
                break;

            case WalletSectionKind.Withdraw:
                SetTitle("WITHDRAW");
                HeaderLabel1.Text = "DESTINATION";
                HeaderLabel2.Text = "STATUS";
                HeaderLabel3.Text = "AMOUNT";
                ConfigureTabs("ALL", "WALLET", "BANK");
                break;

            case WalletSectionKind.Games:
                SetTitle("GAMES");
                HeaderLabel1.Text = "MATCH";
                HeaderLabel2.Text = "RESULT";
                HeaderLabel3.Text = "NET";
                ConfigureTabs("ALL", "PLAYED", "WON", "LOST", "TOURNEY");
                break;
        }
    }

    private void SetTitle(string text)
    {
        SectionTitleBar.Title = text;
    }

    private async Task LoadAsync()
    {
        ReleaseCurrentRows();

        switch (_section)
        {
            case WalletSectionKind.Bonus:
                _bonusItems = await GlobalConstants.MatchMaker.GetWalletBonusHistory();
                RenderBonus();
                break;
            case WalletSectionKind.Deposit:
                _depositItems = await GlobalConstants.MatchMaker.GetWalletDepositHistory();
                RenderDeposit();
                break;
            case WalletSectionKind.Withdraw:
                _withdrawalItems = await GlobalConstants.MatchMaker.GetWalletWithdrawalHistory();
                RenderWithdraw();
                break;
            case WalletSectionKind.Games:
                _gameItems = await GlobalConstants.MatchMaker.GetWalletGameHistory();
                RenderGames();
                break;
        }
    }

    private void RenderBonus()
    {
        var filter = GetCurrentFilter();
        var items = _bonusItems.Where(item => filter switch
        {
            "DAILY" => item.Category.Contains("daily", StringComparison.OrdinalIgnoreCase),
            "REFER" => item.Category.Contains("refer", StringComparison.OrdinalIgnoreCase),
            "AIRDROP" => item.Category.Contains("airdrop", StringComparison.OrdinalIgnoreCase) || item.Description.Contains("airdrop", StringComparison.OrdinalIgnoreCase),
            _ => true
        }).ToList();

        if (items.Count == 0)
        {
            ItemsStack.Children.Add(BuildEmptyState("No bonus records yet."));
            return;
        }

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            ItemsStack.Children.Add(BuildRow(
                (i + 1).ToString(),
                item.Category,
                string.IsNullOrWhiteSpace(item.TransactionReference) ? "-" : item.TransactionReference,
                item.Status,
                ClientGlobalConstants.NormalizeCoins(item.Amount),
                $"{item.Description}{Environment.NewLine}Created: {item.CreatedDate:g}",
                string.IsNullOrWhiteSpace(item.TransactionReference) ? "-" : item.TransactionReference,
                GetStatusColor(item.Status),
                item.Amount >= 0 ? Color.FromArgb("#136F1A") : Color.FromArgb("#8D1010")));
        }
    }

    private void RenderDeposit()
    {
        var filter = GetCurrentFilter();
        var items = _depositItems.Where(item => filter switch
        {
            "DIRECT" => item.Source.Contains("direct", StringComparison.OrdinalIgnoreCase),
            "LOCAL" => item.Source.Contains("local", StringComparison.OrdinalIgnoreCase) || item.Description.Contains("internal", StringComparison.OrdinalIgnoreCase),
            "MANUAL" => item.Source.Contains("manual", StringComparison.OrdinalIgnoreCase),
            _ => true
        }).ToList();

        if (items.Count == 0)
        {
            ItemsStack.Children.Add(BuildEmptyState("No deposit records yet."));
            return;
        }

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            ItemsStack.Children.Add(BuildRow(
                (i + 1).ToString(),
                item.Source,
                string.IsNullOrWhiteSpace(item.ReferenceNumber) ? "-" : item.ReferenceNumber,
                item.Status,
                ClientGlobalConstants.NormalizeCoins(item.Amount),
                $"Created: {item.CreatedDate:g}{Environment.NewLine}Processed: {(item.ProcessedDate.HasValue ? item.ProcessedDate.Value.ToString("g") : "-")}{Environment.NewLine}Payment: {item.PaymentMethod}{Environment.NewLine}Admin Note: {item.AdminNote ?? "-"}{Environment.NewLine}Receipt: {(string.IsNullOrWhiteSpace(item.ReceiptImageUrl) ? "-" : "Attached")}{Environment.NewLine}{Environment.NewLine}{item.Description}",
                string.IsNullOrWhiteSpace(item.ReferenceNumber) ? "-" : item.ReferenceNumber,
                GetStatusColor(item.Status),
                Color.FromArgb("#136F1A")));
        }
    }

    private void RenderWithdraw()
    {
        var filter = GetCurrentFilter();
        var items = _withdrawalItems.Where(item => filter switch
        {
            "WALLET" => item.Method.Contains("wallet", StringComparison.OrdinalIgnoreCase),
            "BANK" => item.Method.Contains("bank", StringComparison.OrdinalIgnoreCase) || item.Method.Contains("manual", StringComparison.OrdinalIgnoreCase),
            _ => true
        }).ToList();

        if (items.Count == 0)
        {
            ItemsStack.Children.Add(BuildEmptyState("No withdrawal records yet."));
            return;
        }

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            ItemsStack.Children.Add(BuildRow(
                (i + 1).ToString(),
                item.Destination,
                string.IsNullOrWhiteSpace(item.TransactionReference) ? "-" : item.TransactionReference,
                item.Status,
                ClientGlobalConstants.NormalizeCoins(item.Amount),
                $"Created: {item.CreatedDate:g}{Environment.NewLine}Processed: {(item.ProcessedDate.HasValue ? item.ProcessedDate.Value.ToString("g") : "-")}{Environment.NewLine}Payout: {item.Method}{Environment.NewLine}Admin Note: {item.AdminNote ?? "-"}{Environment.NewLine}{Environment.NewLine}{item.Description}",
                string.IsNullOrWhiteSpace(item.TransactionReference) ? "-" : item.TransactionReference,
                GetStatusColor(item.Status),
                Color.FromArgb("#8D1010")));
        }
    }

    private void RenderGames()
    {
        var filter = GetCurrentFilter();
        var items = _gameItems.Where(item => filter switch
        {
            "PLAYED" => true,
            "WON" => item.Result.Contains("won", StringComparison.OrdinalIgnoreCase) || item.NetAmount > 0,
            "LOST" => item.Result.Contains("lost", StringComparison.OrdinalIgnoreCase) || item.NetAmount < 0,
            "TOURNEY" => !string.IsNullOrWhiteSpace(item.TournamentName) || item.Mode.Contains("tournament", StringComparison.OrdinalIgnoreCase),
            _ => true
        }).ToList();

        if (items.Count == 0)
        {
            ItemsStack.Children.Add(BuildEmptyState("No game ledger records yet."));
            return;
        }

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var title = string.IsNullOrWhiteSpace(item.TournamentName) ? item.Mode : $"{item.Mode}: {item.TournamentName}";
            ItemsStack.Children.Add(BuildRow(
                (i + 1).ToString(),
                title,
                string.IsNullOrWhiteSpace(item.RoomCode) ? "-" : item.RoomCode,
                item.Result,
                ClientGlobalConstants.NormalizeCoins(item.NetAmount),
                $"Bet: {ClientGlobalConstants.NormalizeCoins(item.BetAmount)}{Environment.NewLine}Room: {item.RoomCode}{Environment.NewLine}Players: {(item.Players.Count == 0 ? "-" : string.Join(", ", item.Players))}{Environment.NewLine}Winners: {(item.Winners.Count == 0 ? "-" : string.Join(", ", item.Winners))}{Environment.NewLine}Date: {item.CreatedDate:g}{Environment.NewLine}{Environment.NewLine}{item.Description}",
                string.IsNullOrWhiteSpace(item.RoomCode) ? "-" : item.RoomCode,
                GetStatusColor(item.Result),
                item.NetAmount >= 0 ? Color.FromArgb("#136F1A") : Color.FromArgb("#8D1010")));
        }
    }

    private View BuildRow(string indexText, string title, string subtitle, string status, string amount, string detailText, string referenceValue, Color statusColor, Color amountColor)
    {
#if ANDROID
        return new NativeWalletHistoryCard(new WalletHistoryRowData
        {
            IndexText = indexText,
            Title = title,
            DateText = subtitle,
            Status = status,
            AmountText = amount,
            DetailAmountText = string.Empty,
            DetailText = detailText,
            ReferenceValue = referenceValue,
            StatusColor = statusColor,
            AmountColor = amountColor
        });
#else
        return new TransactionLongDetailList
        {
            TitleText = title,
            DateText = subtitle,
            StatusText = status,
            AmountText = amount,
            DetailText = detailText,
            ReferenceValue = referenceValue,
            StatusColor = statusColor,
            AmountColor = amountColor
        };
#endif
    }

    private static View BuildEmptyState(string message)
    {
        return new Border
        {
            BackgroundColor = Color.FromArgb("#4A203C6B"),
            Stroke = Colors.Transparent,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Content = new Label
            {
                Text = message,
                TextColor = Colors.White,
                HorizontalTextAlignment = TextAlignment.Center,
                Padding = new Thickness(12)
            }
        };
    }

    private void ConfigureTabs(params string[] labels)
    {
        var tabs = new[] { Tab1, Tab2, Tab3, Tab4, Tab5 };
        TabsGrid.ColumnDefinitions.Clear();

        var visibleCount = labels.Length;
        for (var i = 0; i < visibleCount; i++)
            TabsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        TabsGrid.HorizontalOptions = LayoutOptions.Fill;
        TabsGrid.ClearValue(WidthRequestProperty);

        for (var i = 0; i < tabs.Length; i++)
        {
            if (i < labels.Length)
            {
                tabs[i].IsVisible = true;
                tabs[i].SwitchText = labels[i];
                tabs[i].SwitchSource = tabs[i].SwitchOff;
                tabs[i].ClearValue(HeightRequestProperty);
                tabs[i].ClearValue(MaximumHeightRequestProperty);
                Grid.SetColumn(tabs[i], i);
            }
            else
            {
                tabs[i].IsVisible = false;
                Grid.SetColumn(tabs[i], 0);
            }
        }
    }

    private string GetCurrentFilter()
    {
        return _activeTab?.SwitchText ?? "ALL";
    }

    private void TabRequestedActivate(object sender, EventArgs e)
    {
        if (sender is ImageSwitch activeTab)
        {
            ActivateTab(activeTab);
            RenderCurrentSection();
        }
    }

    private void ActivateTab(ImageSwitch activeTab)
    {
        _activeTab = activeTab;
        foreach (var tab in new[] { Tab1, Tab2, Tab3, Tab4, Tab5 })
        {
            if (!tab.IsVisible)
                continue;

            tab.SwitchSource = tab == activeTab ? tab.SwitchOn : tab.SwitchOff;
        }
    }

    private void RenderCurrentSection()
    {
        ReleaseCurrentRows();

        switch (_section)
        {
            case WalletSectionKind.Bonus:
                RenderBonus();
                break;
            case WalletSectionKind.Deposit:
                RenderDeposit();
                break;
            case WalletSectionKind.Withdraw:
                RenderWithdraw();
                break;
            case WalletSectionKind.Games:
                RenderGames();
                break;
        }
    }

    private static Color GetStatusColor(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return Colors.DarkBlue;

        return status.Contains("approved", StringComparison.OrdinalIgnoreCase) ||
               status.Contains("success", StringComparison.OrdinalIgnoreCase) ||
               status.Contains("completed", StringComparison.OrdinalIgnoreCase) ||
               status.Contains("won", StringComparison.OrdinalIgnoreCase)
            ? Color.FromArgb("#136F1A")
            : status.Contains("pending", StringComparison.OrdinalIgnoreCase) ||
              status.Contains("processing", StringComparison.OrdinalIgnoreCase)
                ? Color.FromArgb("#A36404")
                : status.Contains("reject", StringComparison.OrdinalIgnoreCase) ||
                  status.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
                  status.Contains("lost", StringComparison.OrdinalIgnoreCase)
                    ? Color.FromArgb("#8D1010")
                    : Colors.DarkBlue;
    }

    private void ReleaseCurrentRows()
    {
#if ANDROID
        foreach (var child in ItemsStack.Children.OfType<NativeWalletHistoryCard>())
            child.Handler?.DisconnectHandler();
#endif
        ItemsStack.Children.Clear();
    }

    private void OpenDepositDialog()
    {
#if ANDROID
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity as AndroidX.AppCompat.App.AppCompatActivity;
        if (activity != null)
        {
            var dialog = new LudoClient.Platforms.Android.Popups.AddCashDialogFragment();
            dialog.Show(activity.SupportFragmentManager, "AddCashDialog");
        }
#else
        this.ShowPopup(ClientGlobalConstants.addCash, new PopupOptions { Shape = null });
#endif
    }

    private void OpenWithdrawDialog()
    {
#if ANDROID
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity as AndroidX.AppCompat.App.AppCompatActivity;
        if (activity != null)
        {
            var dialog = new LudoClient.Platforms.Android.Popups.WithdrawDialogFragment();
            dialog.Show(activity.SupportFragmentManager, "WithdrawDialog");
        }
#else
        this.ShowPopup(ClientGlobalConstants.withdrawPopup, new PopupOptions { Shape = null });
#endif
    }
}
