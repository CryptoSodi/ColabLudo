namespace LudoClient.ControlView;

public partial class PlayerBox : ContentView
{
    public BindableProperty PlayerNameProperty = BindableProperty.Create(nameof(PlayerName), typeof(string), typeof(PlayerBox), propertyChanged: (bindable, oldValue, newValue) =>
    {
        var control = (PlayerBox)bindable;
        control.PlayerNameText.Text = (string)newValue;
    });
    public string PlayerName
    {
        get => GetValue(PlayerNameProperty) as string;
        set => SetValue(PlayerNameProperty, value);
    }
    public BindableProperty PlayerImageProperty = BindableProperty.Create(nameof(PlayerImage), typeof(string), typeof(PlayerBox), propertyChanged: (bindable, oldValue, newValue) =>
    {
        var control = (PlayerBox)bindable;
        control.PlayerImageItem.PlayerImage = (string)newValue;
    });
    public string PlayerImage
    {
        get => GetValue(PlayerImageProperty) as string;
        set => SetValue(PlayerImageProperty, value);
    }
    public PlayerBox()
    {
        InitializeComponent();
    }
}