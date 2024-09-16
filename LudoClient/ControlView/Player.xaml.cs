namespace LudoClient.ControlView;

public partial class Player : ContentView
{
    public BindableProperty PlayerImageProperty = BindableProperty.Create(nameof(PlayerImage), typeof(string), typeof(Player), propertyChanged: (bindable, oldValue, newValue) =>
    {
        var control = (Player)bindable;
        control.PlayerImageItem.Source = (string)newValue;
    });
    public string PlayerImage
    {
        get => GetValue(PlayerImageProperty) as string;
        set => SetValue(PlayerImageProperty, value);
    }
    public Player()
    {
        InitializeComponent();
    }
}