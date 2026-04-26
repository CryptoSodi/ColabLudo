namespace LudoClient.ControlView;

public partial class TransactionLongDetailList : ContentView
{
    public static readonly BindableProperty IndexTextProperty =
        BindableProperty.Create(nameof(IndexText), typeof(string), typeof(TransactionLongDetailList), string.Empty, propertyChanged: OnTextChanged);

    public static readonly BindableProperty TitleTextProperty =
        BindableProperty.Create(nameof(TitleText), typeof(string), typeof(TransactionLongDetailList), string.Empty, propertyChanged: OnTextChanged);

    public static readonly BindableProperty DateTextProperty =
        BindableProperty.Create(nameof(DateText), typeof(string), typeof(TransactionLongDetailList), string.Empty, propertyChanged: OnTextChanged);

    public static readonly BindableProperty StatusTextProperty =
        BindableProperty.Create(nameof(StatusText), typeof(string), typeof(TransactionLongDetailList), string.Empty, propertyChanged: OnTextChanged);

    public static readonly BindableProperty AmountTextProperty =
        BindableProperty.Create(nameof(AmountText), typeof(string), typeof(TransactionLongDetailList), string.Empty, propertyChanged: OnTextChanged);

    public static readonly BindableProperty DetailTextProperty =
        BindableProperty.Create(nameof(DetailText), typeof(string), typeof(TransactionLongDetailList), string.Empty, propertyChanged: OnTextChanged);

    public static readonly BindableProperty ReferenceTitleProperty =
        BindableProperty.Create(nameof(ReferenceTitle), typeof(string), typeof(TransactionLongDetailList), "TRANSACTION ID :", propertyChanged: OnTextChanged);

    public static readonly BindableProperty ReferenceValueProperty =
        BindableProperty.Create(nameof(ReferenceValue), typeof(string), typeof(TransactionLongDetailList), string.Empty, propertyChanged: OnTextChanged);

    public static readonly BindableProperty StatusColorProperty =
        BindableProperty.Create(nameof(StatusColor), typeof(Color), typeof(TransactionLongDetailList), Colors.DarkBlue, propertyChanged: OnColorChanged);

    public static readonly BindableProperty AmountColorProperty =
        BindableProperty.Create(nameof(AmountColor), typeof(Color), typeof(TransactionLongDetailList), Colors.Black, propertyChanged: OnColorChanged);

    public TransactionLongDetailList()
    {
        InitializeComponent();
        ApplyState();
    }

    public string IndexText
    {
        get => (string)GetValue(IndexTextProperty);
        set => SetValue(IndexTextProperty, value);
    }

    public string TitleText
    {
        get => (string)GetValue(TitleTextProperty);
        set => SetValue(TitleTextProperty, value);
    }

    public string DateText
    {
        get => (string)GetValue(DateTextProperty);
        set => SetValue(DateTextProperty, value);
    }

    public string StatusText
    {
        get => (string)GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    public string AmountText
    {
        get => (string)GetValue(AmountTextProperty);
        set => SetValue(AmountTextProperty, value);
    }

    public string DetailText
    {
        get => (string)GetValue(DetailTextProperty);
        set => SetValue(DetailTextProperty, value);
    }

    public string ReferenceTitle
    {
        get => (string)GetValue(ReferenceTitleProperty);
        set => SetValue(ReferenceTitleProperty, value);
    }

    public string ReferenceValue
    {
        get => (string)GetValue(ReferenceValueProperty);
        set => SetValue(ReferenceValueProperty, value);
    }

    public Color StatusColor
    {
        get => (Color)GetValue(StatusColorProperty);
        set => SetValue(StatusColorProperty, value);
    }

    public Color AmountColor
    {
        get => (Color)GetValue(AmountColorProperty);
        set => SetValue(AmountColorProperty, value);
    }

    private void Expand_Clicked(object sender, EventArgs e)
    {
        if (ExpandSheet.Padding.Top > 0)
        {
            ExpandSheet.IsVisible = false;
            ExpandSheet.Margin = new Thickness(10, 0, 10, 0);
            ExpandSheet.Padding = new Thickness(0, 0, 0, 0);
            SheetDirection.Source = "arr_down.webp";
            return;
        }
        else
        {
            ExpandSheet.Margin = new Thickness(10, 0, 10, 0);
            ExpandSheet.IsVisible = true;
            SheetDirection.Source = "arr_up.webp";
            ExpandSheet.Padding = new Thickness(0, (SubSheet.Height - 10), 0, 0);
        }
    }

    private static void OnTextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((TransactionLongDetailList)bindable).ApplyState();
    }

    private static void OnColorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((TransactionLongDetailList)bindable).ApplyState();
    }

    private void ApplyState()
    {
        IndexTextLabel.Text = IndexText;
        TitleTextLabel.Text = TitleText;
        DateTextLabel.Text = TrimHeaderText(DateText, 26);
        AmountTextLabel.Text = AmountText;
        StatusBackground.Source = GetStatusAsset(StatusText);
        AmountTextLabel.TextColor = AmountColor;
        RebuildDetails();
    }

    private static string TrimHeaderText(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
            return value;

        return $"{value[..maxLength]}...";
    }

    private void RebuildDetails()
    {
        PrimaryDetailsStack.Children.Clear();
        SecondaryDetailsStack.Children.Clear();

        var detailLines = (DetailText ?? string.Empty)
            .Split([Environment.NewLine], StringSplitOptions.None)
            .Select(line => line.Trim())
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
            {
                primaryLines.Add(line);
            }
            else
            {
                secondaryLines.Add(line);
            }
        }

        if (!primaryLines.Any(static line => line.StartsWith("Status:", StringComparison.OrdinalIgnoreCase)))
            primaryLines.Add($"Status: {StatusText}");

        foreach (var line in primaryLines)
            PrimaryDetailsStack.Children.Add(CreateDetailRow(line));

        foreach (var line in secondaryLines)
            SecondaryDetailsStack.Children.Add(CreateDetailRow(line));

        SecondaryDetailsStack.Children.Add(CreateDetailRow($"{ReferenceTitle.TrimEnd()} {ReferenceValue}".Trim()));

        if (PrimaryDetailsStack.Children.Count == 0)
            PrimaryDetailsStack.Children.Add(CreateDetailRow("DETAIL: -"));
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

    private static View CreateDetailRow(string line)
    {
        var parts = line.Split(':', 2, StringSplitOptions.TrimEntries);
        var hasValue = parts.Length == 2;
        var labelText = hasValue ? parts[0].ToUpperInvariant() : "DETAIL";
        var valueText = hasValue ? parts[1] : line;

        var valueLabel = new Label
        {
            Text = valueText,
            TextColor = GetValueColor(labelText, valueText),
            FontSize = 10,
            VerticalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Start,
            HorizontalOptions = LayoutOptions.Start,
            LineBreakMode = LineBreakMode.WordWrap
        };
        Grid.SetColumn(valueLabel, 1);

        var label = new Label
        {
            Text = labelText,
            TextColor = Color.FromArgb("#A99BEF"),
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Start,
            HorizontalOptions = LayoutOptions.Start
        };
        Grid.SetColumn(label, 1);
        Grid.SetColumn(valueLabel, 2);

        var bullet = CreateBulletLabel();

        return new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(new GridLength(16)),
                new ColumnDefinition(new GridLength(120)),
                new ColumnDefinition(GridLength.Star)
            ],
            ColumnSpacing = 12,
            HorizontalOptions = LayoutOptions.Fill,
            Children =
            {
                bullet,
                label,
                valueLabel
            }
        };
    }

    private static Label CreateBulletLabel()
    {
        return new Label
        {
            Text = "•",
            TextColor = Color.FromArgb("#5FD4FF"),
            FontSize = 12,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
    }

    private static Color GetValueColor(string label, string value)
    {
        if (label.Contains("STATUS", StringComparison.OrdinalIgnoreCase))
            return GetStatusTextColor(value);

        if (label.Contains("PAYMENT", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("PAYOUT", StringComparison.OrdinalIgnoreCase) ||
            label.Contains("RECEIPT", StringComparison.OrdinalIgnoreCase))
            return Color.FromArgb("#F7B233");

        return Colors.White;
    }

    private static string GetStatusAsset(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return "pending.webp";

        if (status.Contains("success", StringComparison.OrdinalIgnoreCase))
            return "success.webp";
        if (status.Contains("complete", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("approved", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("won", StringComparison.OrdinalIgnoreCase))
            return "complete.webp";
        if (status.Contains("processing", StringComparison.OrdinalIgnoreCase))
            return "processing.webp";
        if (status.Contains("reject", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("fail", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("lost", StringComparison.OrdinalIgnoreCase))
            return "failed.webp";

        return "pending.webp";
    }

    private static Color GetStatusTextColor(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return Colors.White;

        if (status.Contains("pending", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("processing", StringComparison.OrdinalIgnoreCase))
            return Color.FromArgb("#F7B233");

        if (status.Contains("reject", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("fail", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("lost", StringComparison.OrdinalIgnoreCase))
            return Color.FromArgb("#FF8D8D");

        return Color.FromArgb("#F7B233");
    }
}
