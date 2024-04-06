namespace LudoClient.ControlView;

public partial class MessageBoxConfirm : ContentView
{
    public BindableProperty SettingTextProperty = BindableProperty.Create(nameof(SettingText), typeof(string), typeof(MessageBoxConfirm), propertyChanged: (bindable, oldValue, newValue) =>
    {
        var control = (MessageBoxConfirm)bindable;
        control.SettingTextView.Text = (string)newValue;
    });

    public BindableProperty SettingTitleProperty = BindableProperty.Create(nameof(SettingTitle), typeof(string), typeof(MessageBoxConfirm), propertyChanged: (bindable, oldValue, newValue) =>
    {
        var control = (MessageBoxConfirm)bindable;
        control.SettingTitleView.Title = (string)newValue;

    });

    public string SettingText
    {
        get => GetValue(SettingTextProperty) as string;
        set => SetValue(SettingTextProperty, value);
    }

    public string SettingTitle
    {
        get => GetValue(SettingTitleProperty) as string;
        set => SetValue(SettingTitleProperty, value);
    }

    public MessageBoxConfirm()
	{
		InitializeComponent();
	}
}