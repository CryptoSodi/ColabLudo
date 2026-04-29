using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace LudoClient.Platforms.Android;

public class NativeWalletHistoryCard : Microsoft.Maui.Controls.View
{
    public WalletHistoryRowData RowData { get; set; }

    public NativeWalletHistoryCard(WalletHistoryRowData rowData)
    {
        RowData = rowData;
        HorizontalOptions = Microsoft.Maui.Controls.LayoutOptions.Fill;
    }

}

public class NativeWalletHistoryCardHandler : ViewHandler<NativeWalletHistoryCard, WalletHistoryDetailView>
{
    public static PropertyMapper<NativeWalletHistoryCard, NativeWalletHistoryCardHandler> Mapper = new(ViewHandler.ViewMapper) { [nameof(NativeWalletHistoryCard.RowData)] = MapRowData, };

    public NativeWalletHistoryCardHandler() : base(Mapper)
    {
    }

    protected override WalletHistoryDetailView CreatePlatformView()
    {
        var view = new WalletHistoryDetailView(Context);
        view.LayoutParameters = new ViewGroup.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent);

        return view;
    }

    protected override void ConnectHandler(WalletHistoryDetailView platformView)
    {
        base.ConnectHandler(platformView);
        UpdatePlatformView();
    }

    protected override void DisconnectHandler(WalletHistoryDetailView platformView)
    {
        platformView.Release();
        base.DisconnectHandler(platformView);
    }

    static void MapRowData(NativeWalletHistoryCardHandler handler, NativeWalletHistoryCard view)
    {
        handler.UpdatePlatformView();
    }

    void UpdatePlatformView()
    {
        if (PlatformView != null && VirtualView?.RowData != null)
        {
            PlatformView.SetDetails(VirtualView.RowData);
            PlatformView.RequestLayout();
        }
    }

    public override Microsoft.Maui.Graphics.Size GetDesiredSize(double widthConstraint, double heightConstraint)
    {
        if (PlatformView == null)
            return Microsoft.Maui.Graphics.Size.Zero;

        var widthPx = widthConstraint > 0 && !double.IsInfinity(widthConstraint)
            ? (int)Context.ToPixels(widthConstraint)
            : Context.Resources?.DisplayMetrics?.WidthPixels ?? 0;
        var widthSpec = global::Android.Views.View.MeasureSpec.MakeMeasureSpec(widthPx, global::Android.Views.MeasureSpecMode.Exactly);
        var heightSpec = global::Android.Views.View.MeasureSpec.MakeMeasureSpec(
            0,
            global::Android.Views.MeasureSpecMode.Unspecified);

        PlatformView.Measure(widthSpec, heightSpec);

        return new Microsoft.Maui.Graphics.Size(
            Context.FromPixels(PlatformView.MeasuredWidth),
            Context.FromPixels(PlatformView.MeasuredHeight));
    }

}

[Register("ludoclient.platforms.android.WalletHistoryDetailView")]
public class WalletHistoryDetailView : FrameLayout
{
    private FrameLayout _detailContainer = null!; private TextView _indexText = null!; private TextView _titleText = null!; private TextView _dateText = null!; private ImageView _statusImage = null!; private TextView _amountText = null!; private ImageView _arrowImage = null!; private LinearLayout _primaryDetails = null!; private ImageView _detailDivider = null!; private LinearLayout _secondaryDetails = null!; private bool _isExpanded;

    public WalletHistoryDetailView(Context context) : base(context)
    {
        Initialize(context);
    }

    public WalletHistoryDetailView(Context context, IAttributeSet attrs) : base(context, attrs)
    {
        Initialize(context);
    }

    public WalletHistoryDetailView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
    {
        Initialize(context);
    }

    protected WalletHistoryDetailView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
    }

    private void Initialize(Context context)
    {
        var view = LayoutInflater.FromContext(context).Inflate(Resource.Layout.item_wallet_history_detail, this, true);

        _detailContainer = view.FindViewById<FrameLayout>(Resource.Id.WalletRowDetails)!;
        _indexText = view.FindViewById<TextView>(Resource.Id.WalletRowIndex)!;
        _titleText = view.FindViewById<TextView>(Resource.Id.WalletRowTitle)!;
        _dateText = view.FindViewById<TextView>(Resource.Id.WalletRowDate)!;
        _statusImage = view.FindViewById<ImageView>(Resource.Id.WalletRowStatus)!;
        _amountText = view.FindViewById<TextView>(Resource.Id.WalletRowAmount)!;
        _arrowImage = view.FindViewById<ImageView>(Resource.Id.WalletRowArrow)!;
        _primaryDetails = view.FindViewById<LinearLayout>(Resource.Id.WalletRowPrimaryDetails)!;
        _detailDivider = view.FindViewById<ImageView>(Resource.Id.WalletRowDetailDivider)!;
        _secondaryDetails = view.FindViewById<LinearLayout>(Resource.Id.WalletRowSecondaryDetails)!;

        var clickTarget = view.FindViewById<LinearLayout>(Resource.Id.WalletRowSummaryClickTarget)!;
        clickTarget.Click += OnSummaryClicked;
    }

    public void SetDetails(WalletHistoryRowData rowData)
    {
        _indexText.Text = rowData.IndexText;
        _titleText.Text = rowData.Title;
        _dateText.Text = TrimHeaderText(rowData.DateText, 26);
        _amountText.Text = rowData.AmountText;
        _amountText.SetTextColor(rowData.AmountColor.ToPlatform());
        _statusImage.SetImageResource(GetStatusAsset(rowData.Status));

        RebuildDetails(rowData);

        if (_isExpanded)
            Expand();
        else
            Collapse();
    }

    public void Release()
    {
    }

    private void OnSummaryClicked(object? sender, EventArgs e)
    {
        if (_isExpanded)
            Collapse();
        else
            Expand();
    }

    private void Expand()
    {
        _isExpanded = true;
        _detailContainer.Visibility = ViewStates.Visible;
        _arrowImage.SetImageResource(Resource.Drawable.arr_up);
        RequestLayout();
    }

    private void Collapse()
    {
        _isExpanded = false;
        _detailContainer.Visibility = ViewStates.Gone;
        _arrowImage.SetImageResource(Resource.Drawable.arr_down);
        RequestLayout();
    }

    private void RebuildDetails(WalletHistoryRowData rowData)
    {
        _primaryDetails.RemoveAllViews();
        _secondaryDetails.RemoveAllViews();

        var detailLines = (rowData.DetailText ?? string.Empty)
            .Split(new[] { Environment.NewLine }, StringSplitOptions.None)
            .Select(static line => line.Trim())
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        var primaryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Created",
        "Payment",
        "Receipt",
        "Admin Note",
        "Status"
    };

        var primaryLines = new List<string>();
        var secondaryLines = new List<string>();

        foreach (var line in detailLines)
        {
            var key = ExtractKey(line);
            if (key != null && primaryKeys.Contains(key))
                primaryLines.Add(line);
            else
                secondaryLines.Add(line);
        }

        if (!primaryLines.Any(static line => line.StartsWith("Status:", StringComparison.OrdinalIgnoreCase)))
            primaryLines.Add($"Status: {rowData.Status}");

        foreach (var line in primaryLines)
            _primaryDetails.AddView(CreateDetailRow(line));

        foreach (var line in secondaryLines)
            _secondaryDetails.AddView(CreateDetailRow(line));

        _secondaryDetails.AddView(CreateDetailRow($"TRANSACTION ID: {rowData.ReferenceValue}".Trim()));
        _detailDivider.Visibility = _secondaryDetails.ChildCount > 0 ? ViewStates.Visible : ViewStates.Gone;
    }

    private LinearLayout CreateDetailRow(string line)
    {
        var parts = line.Split(':', 2, StringSplitOptions.TrimEntries);
        var hasValue = parts.Length == 2;
        var labelText = hasValue ? parts[0].ToUpperInvariant() : "DETAIL";
        var valueText = hasValue ? parts[1] : line;

        var row = new LinearLayout(Context)
        {
            Orientation = Orientation.Horizontal
        };
        row.SetGravity(GravityFlags.CenterVertical);
        row.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        {
            TopMargin = Dp(2),
            BottomMargin = Dp(2)
        };

        var bullet = new TextView(Context)
        {
            Text = "\u2022",
            TextSize = 12f
        };
        bullet.SetTextColor(global::Android.Graphics.Color.Rgb(95, 212, 255));
        bullet.SetPadding(0, 0, Dp(10), 0);

        var label = new TextView(Context)
        {
            Text = labelText,
            TextSize = 10f,
            Typeface = global::Android.Graphics.Typeface.DefaultBold
        };
        label.SetTextColor(global::Android.Graphics.Color.Rgb(169, 155, 239));
        label.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f);

        var value = new TextView(Context)
        {
            Text = valueText,
            TextSize = 10f
        };
        value.SetTextColor(GetValueColor(labelText, valueText));
        if (labelText.Equals("STATUS", StringComparison.OrdinalIgnoreCase))
            value.Typeface = global::Android.Graphics.Typeface.DefaultBold;
        value.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1.35f);

        row.AddView(bullet);
        row.AddView(label);
        row.AddView(value);
        return row;
    }

    private static string? ExtractKey(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        var separatorIndex = line.IndexOf(':');
        if (separatorIndex <= 0)
            return null;

        return line[..separatorIndex].Trim();
    }

    private static string TrimHeaderText(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
            return value;

        return $"{value[..maxLength]}...";
    }

    private static int GetStatusAsset(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return Resource.Drawable.pending;

        if (status.Contains("success", StringComparison.OrdinalIgnoreCase))
            return Resource.Drawable.success;
        if (status.Contains("complete", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("approved", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("won", StringComparison.OrdinalIgnoreCase))
            return Resource.Drawable.complete;
        if (status.Contains("processing", StringComparison.OrdinalIgnoreCase))
            return Resource.Drawable.processing;
        if (status.Contains("reject", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("fail", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("lost", StringComparison.OrdinalIgnoreCase))
            return Resource.Drawable.failed;

        return Resource.Drawable.pending;
    }

    private static global::Android.Graphics.Color GetValueColor(string label, string value)
    {
        if (label.Contains("STATUS", StringComparison.OrdinalIgnoreCase))
            return GetStatusTextColor(value);

        if (label.Contains("PAYMENT", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("PAYOUT", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("RECEIPT", StringComparison.OrdinalIgnoreCase))
            return global::Android.Graphics.Color.Rgb(247, 178, 51);

        return global::Android.Graphics.Color.White;
    }

    private static global::Android.Graphics.Color GetStatusTextColor(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return global::Android.Graphics.Color.White;

        if (status.Contains("pending", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("processing", StringComparison.OrdinalIgnoreCase))
            return global::Android.Graphics.Color.Rgb(247, 178, 51);

        if (status.Contains("reject", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("fail", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("lost", StringComparison.OrdinalIgnoreCase))
            return global::Android.Graphics.Color.Rgb(255, 141, 141);

        return global::Android.Graphics.Color.Rgb(247, 178, 51);
    }

    private int Dp(float value)
    {
        var density = Context.Resources?.DisplayMetrics?.Density ?? 1f;
        return (int)(value * density);
    }
}
