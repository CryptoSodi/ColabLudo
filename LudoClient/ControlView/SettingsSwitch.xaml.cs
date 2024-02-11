namespace LudoClient.ControlView;

public partial class SettingsSwitch : ContentView
{
    public BindableProperty SettingTextProperty = BindableProperty.Create(nameof(SettingText), typeof(string), typeof(SettingsSwitch), propertyChanged: (bindable, oldValue, newValue) =>
    {
        var control = (SettingsSwitch)bindable;
        control.SettingTextView.Text = (string)newValue;
    });
    public string SettingText
    {
        get => GetValue(SettingTextProperty) as string;
        set => SetValue(SettingTextProperty, value);
    }
    public SettingsSwitch()
	{
		InitializeComponent();
	}
}