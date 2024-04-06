namespace LudoClient.ControlView;

public partial class VolumeSwitch : ContentView
{
    public BindableProperty SettingTextProperty = BindableProperty.Create(nameof(SettingText), typeof(string), typeof(VolumeSwitch), propertyChanged: (bindable, oldValue, newValue) =>
    {
        var control = (VolumeSwitch)bindable;
        control.SettingTextView.Text = (string)newValue;
    });
    public string SettingText
    {
        get => GetValue(SettingTextProperty) as string;
        set => SetValue(SettingTextProperty, value);
    }
    public VolumeSwitch()
    {
        InitializeComponent();
    }
}