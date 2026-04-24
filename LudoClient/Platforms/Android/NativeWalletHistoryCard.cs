using Android.Content;
using Android.Graphics.Drawables;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.ConstraintLayout.Widget;
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
        Margin = new Thickness(0);
    }
}

public class NativeWalletHistoryCardHandler : ViewHandler<NativeWalletHistoryCard, WalletHistoryDetailView>
{
    public static PropertyMapper<NativeWalletHistoryCard, NativeWalletHistoryCardHandler> Mapper = new(ViewHandler.ViewMapper)
    {
        [nameof(NativeWalletHistoryCard.RowData)] = MapRowData,
    };

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

        var widthSpec = global::Android.Views.View.MeasureSpec.MakeMeasureSpec(
            (int)Context.ToPixels(widthConstraint),
            global::Android.Views.MeasureSpecMode.AtMost);
        var heightSpec = global::Android.Views.View.MeasureSpec.MakeMeasureSpec(
            0,
            global::Android.Views.MeasureSpecMode.Unspecified);

        PlatformView.Measure(widthSpec, heightSpec);

        return new Microsoft.Maui.Graphics.Size(
            Context.FromPixels(PlatformView.MeasuredWidth),
            Context.FromPixels(PlatformView.MeasuredHeight) > 0 ? Context.FromPixels(PlatformView.MeasuredHeight) : 72);
    }
}

[Register("ludoclient.platforms.android.WalletHistoryDetailView")]
public class WalletHistoryDetailView : LinearLayout
{
    private TextView _indexText = null!;
    private TextView _titleText = null!;
    private TextView _dateText = null!;
    private TextView _statusText = null!;
    private TextView _amountText = null!;
    private ImageView _arrow = null!;
    private FrameLayout _detailContainer = null!;
    private global::Android.Views.View _detailBackdrop = null!;
    private TextView _detailText = null!;
    private TextView _referenceValue = null!;
    private TextView _expandedStatus = null!;
    private bool _isExpanded;

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
        Orientation = Orientation.Vertical;
        var view = LayoutInflater.FromContext(context).Inflate(Resource.Layout.item_wallet_history_detail, this, true);

        _titleText = view.FindViewById<TextView>(Resource.Id.WalletRowTitle);
        _indexText = view.FindViewById<TextView>(Resource.Id.WalletRowIndex);
        _dateText = view.FindViewById<TextView>(Resource.Id.WalletRowDate);
        _statusText = view.FindViewById<TextView>(Resource.Id.WalletRowStatus);
        _amountText = view.FindViewById<TextView>(Resource.Id.WalletRowAmount);
        _arrow = view.FindViewById<ImageView>(Resource.Id.WalletRowArrow);
        _detailContainer = view.FindViewById<FrameLayout>(Resource.Id.WalletRowDetails);
        _detailBackdrop = view.FindViewById<global::Android.Views.View>(Resource.Id.WalletRowDetailBackdrop);
        _detailText = view.FindViewById<TextView>(Resource.Id.WalletRowDetailText);
        _referenceValue = view.FindViewById<TextView>(Resource.Id.WalletRowReferenceValue);
        _expandedStatus = view.FindViewById<TextView>(Resource.Id.WalletRowExpandedStatus);

        view.FindViewById<ConstraintLayout>(Resource.Id.WalletRowSummary).Click += (_, _) => ToggleExpanded();
    }

    public void SetDetails(WalletHistoryRowData rowData)
    {
        _titleText.Text = rowData.Title;
        _indexText.Text = rowData.IndexText;
        _dateText.Text = rowData.DateText;
        _statusText.Text = FormatStatus(rowData.Status);
        _amountText.Text = rowData.AmountText;
        _detailText.Text = rowData.DetailText;
        _referenceValue.Text = rowData.ReferenceValue;
        _expandedStatus.Text = FormatStatus(rowData.Status);

        _amountText.SetTextColor(rowData.AmountColor.ToPlatform());
        ApplyStatusBackground(_statusText, rowData.StatusColor.ToPlatform());
        ApplyDetailBackground();
        Elevation = 0f;
        viewSetElevation();

        if (!_isExpanded)
            Collapse();
    }

    public void Release()
    {
    }

    private void ToggleExpanded()
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
        _arrow.SetImageResource(Resource.Drawable.arr_up);
        RequestLayout();
    }

    private void Collapse()
    {
        _isExpanded = false;
        _detailContainer.Visibility = ViewStates.Gone;
        _arrow.SetImageResource(Resource.Drawable.arr_down);
        RequestLayout();
    }

    private static void ApplyStatusBackground(TextView textView, global::Android.Graphics.Color color)
    {
        var drawable = new GradientDrawable();
        drawable.SetCornerRadius(12f);
        drawable.SetColor(color);
        textView.Background = drawable;
    }

    private static string FormatStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return "-";

        return status.Trim() switch
        {
            var s when s.Equals("Pending", StringComparison.OrdinalIgnoreCase) => "Pending",
            var s when s.Equals("Completed", StringComparison.OrdinalIgnoreCase) => "Completed",
            var s when s.Equals("Approved", StringComparison.OrdinalIgnoreCase) => "Approved",
            var s when s.Equals("Rejected", StringComparison.OrdinalIgnoreCase) => "Rejected",
            _ => status.Trim()
        };
    }

    private void ApplyDetailBackground()
    {
        var drawable = new GradientDrawable();
        drawable.SetCornerRadii(new float[]
        {
            18f, 18f,
            18f, 18f,
            18f, 18f,
            18f, 18f
        });
        drawable.SetColor(global::Android.Graphics.Color.Rgb(24, 52, 96));
        drawable.SetStroke(2, global::Android.Graphics.Color.Rgb(46, 84, 145));
        _detailBackdrop.Background = drawable;
    }

    private void viewSetElevation()
    {
        var summary = FindViewById<ConstraintLayout>(Resource.Id.WalletRowSummary);
        var density = Context.Resources?.DisplayMetrics?.Density ?? 1f;
        summary.Elevation = 8f * density;
        if (summary.Background == null)
        {
            var drawable = new GradientDrawable();
            drawable.SetColor(global::Android.Graphics.Color.Transparent);
            summary.Background = drawable;
        }
    }
}
