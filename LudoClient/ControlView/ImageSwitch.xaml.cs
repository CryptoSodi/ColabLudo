namespace LudoClient.ControlView;

public partial class ImageSwitch : ContentView
{
    public static readonly BindableProperty SwitchSourceProperty = BindableProperty.Create(
        nameof(SwitchSource),
        typeof(string),
        typeof(ImageSwitch),
        propertyChanged: OnSwitchSourceChanged);

    public static readonly BindableProperty SwitchOnProperty = BindableProperty.Create(
        nameof(SwitchOn),
        typeof(string),
        typeof(ImageSwitch),
        default(string));

    public static readonly BindableProperty SwitchOffProperty = BindableProperty.Create(
        nameof(SwitchOff),
        typeof(string),
        typeof(ImageSwitch),
        default(string));

    public string SwitchSource
    {
        get => (string)GetValue(SwitchSourceProperty);
        set => SetValue(SwitchSourceProperty, value);
    }

    public string SwitchOn
    {
        get => (string)GetValue(SwitchOnProperty);
        set => SetValue(SwitchOnProperty, value);
    }

    public string SwitchOff
    {
        get => (string)GetValue(SwitchOffProperty);
        set => SetValue(SwitchOffProperty, value);
    }

    public bool SwitchState { get; set; } = true;

    public ImageSwitch()
    {
        InitializeComponent();
        SwitchSource = SwitchOn;
    }

    protected override void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == SwitchOnProperty.PropertyName && SwitchState)
        {
            SwitchSource = SwitchOn;
        }
        else if (propertyName == SwitchOffProperty.PropertyName && !SwitchState)
        {
            SwitchSource = SwitchOff;
        }
    }

    private static void OnSwitchSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (ImageSwitch)bindable;
        control.SwitchImage.ImageSource = ImageSource.FromFile((string)newValue);
    }

    private void SwitchImage_Clicked(object sender, EventArgs e)
    {
        SwitchState = !SwitchState;
        SwitchSource = SwitchState ? SwitchOn : SwitchOff;
    }
}
