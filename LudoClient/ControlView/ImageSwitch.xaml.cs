using LudoClient.Constants;
using Microsoft.Maui.Controls.PlatformConfiguration.TizenSpecific;

namespace LudoClient.ControlView;

public partial class ImageSwitch : ContentView
{
    // Event to notify when the switch is toggled
    public event EventHandler SwitchToggled;
    // Event to notify when the switch wants to be activated (for tab functionality)
    public event EventHandler<EventArgs> RequestActivate;
    public static readonly BindableProperty SwitchSourceProperty = BindableProperty.Create(nameof(SwitchSource), typeof(string), typeof(ImageSwitch), propertyChanged: OnSwitchSourceChanged);
    public static readonly BindableProperty SwitchOnProperty = BindableProperty.Create(nameof(SwitchOn), typeof(string), typeof(ImageSwitch), default(string));
    public static readonly BindableProperty SwitchOffProperty = BindableProperty.Create(nameof(SwitchOff), typeof(string), typeof(ImageSwitch), default(string));
    public static readonly BindableProperty IsIndependentProperty = BindableProperty.Create(nameof(IsIndependent), typeof(bool), typeof(ImageSwitch), defaultValue: true);
    public BindableProperty SwitchTextProperty = BindableProperty.Create(nameof(SwitchText), typeof(string), typeof(ImageSwitch), propertyChanged: (bindable, oldValue, newValue) =>
    {
        var control = (ImageSwitch)bindable;
        control.DisplayText.Text = (string)newValue;
    });
    public static readonly BindableProperty IsActiveProperty = BindableProperty.Create(nameof(IsActive), typeof(bool), typeof(ImageSwitch), defaultValue: false);
    public string SwitchText
    {
        get => GetValue(SwitchTextProperty) as string;
        set => SetValue(SwitchTextProperty, value);
    }

    public bool IsIndependent
    {
        get => (bool)GetValue(IsIndependentProperty);
        set => SetValue(IsIndependentProperty, value);
    }
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
    public bool IsActive { get; set; } = false;

    public ImageSwitch()
    {
        InitializeComponent();
        UpdateSwitchSource();
    }
    public void UpdateSwitchSource()
    {
        SwitchSource = SwitchState ? SwitchOn : SwitchOff;
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
        if (IsIndependent)
        {
            // Toggle own state
            SwitchState = !SwitchState;
            UpdateSwitchSource();
            // Raise the SwitchToggled event to notify subscribers
            SwitchToggled?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            // Notify parent container (for tab functionality)
            RequestActivate?.Invoke(this, EventArgs.Empty);
        }
    } 
}
