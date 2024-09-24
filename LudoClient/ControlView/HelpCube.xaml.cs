namespace LudoClient.ControlView;

public partial class HelpCube : ContentView
{
    // Bindable Property for ImageSource
    public static readonly BindableProperty ImageSourceProperty =
        BindableProperty.Create(nameof(ImageSource), typeof(string), typeof(HelpCube), default(string));

    public string ImageSource
    {
        get => (string)GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    // Bindable Property for TextHeading
    public static readonly BindableProperty TextHeadingProperty =
        BindableProperty.Create(nameof(TextHeading), typeof(string), typeof(HelpCube), default(string));

    public string TextHeading
    {
        get => (string)GetValue(TextHeadingProperty);
        set => SetValue(TextHeadingProperty, value);
    }

    // Bindable Property for TextSub
    public static readonly BindableProperty TextSubProperty =
        BindableProperty.Create(nameof(TextSub), typeof(string), typeof(HelpCube), default(string));

    public string TextSub
    {
        get => (string)GetValue(TextSubProperty);
        set => SetValue(TextSubProperty, value);
    }

    public HelpCube()
    {
        InitializeComponent();
        BindingContext = this; // Set the BindingContext to the control itself
    }
}