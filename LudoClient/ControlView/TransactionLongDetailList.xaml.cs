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
        ExpandedStatusLabel.Text = $" {StatusText}";
        AmountTextLabel.Text = AmountText;
        DetailTextLabel.Text = DetailText;
        ReferenceTitleLabel.Text = ReferenceTitle;
        ReferenceValueLabel.Text = $" {ReferenceValue}";
        if(StatusText == "Success")
        StatusBackground.Source = "success.webp";
        AmountTextLabel.TextColor = AmountColor;
    }

    private static string TrimHeaderText(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
            return value;

        return $"{value[..maxLength]}...";
    }
}
