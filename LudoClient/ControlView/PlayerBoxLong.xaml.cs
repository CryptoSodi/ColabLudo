namespace LudoClient.ControlView;

public partial class PlayerBoxLong : ContentView
{
    public BindableProperty PlayerNameProperty = BindableProperty.Create(nameof(PlayerName), typeof(string), typeof(PlayerBoxLong), propertyChanged: (bindable, oldValue, newValue) =>
    {
        var control = (PlayerBoxLong)bindable;
        control.PlayerNameText.Text = (string)newValue;
    });
    public string PlayerName
    {
        get => GetValue(PlayerNameProperty) as string;
        set => SetValue(PlayerNameProperty, value);
    }
    public BindableProperty PlayerImageProperty = BindableProperty.Create(nameof(PlayerImage), typeof(string), typeof(PlayerBoxLong), propertyChanged: (bindable, oldValue, newValue) =>
    {
        var control = (PlayerBoxLong)bindable;
        control.PlayerImageItem.Source = (string)newValue;
    });
    public string PlayerImage
    {
        get => GetValue(PlayerImageProperty) as string;
        set => SetValue(PlayerImageProperty, value);
    }
    public PlayerBoxLong()
	{
		InitializeComponent();
	}
}