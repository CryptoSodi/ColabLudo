namespace LudoClient.ControlView;

public partial class PlayerSeat : ContentView
{
    public delegate void DiceClickedHandler(string SeatName);
    public event DiceClickedHandler OnDiceClicked;

    public BindableProperty PlayerImageProperty = BindableProperty.Create(nameof(PlayerBG), typeof(string), typeof(PlayerSeat), propertyChanged: (bindable, oldValue, newValue) =>
    {
        var control = (PlayerSeat)bindable;
        control.playerBG.ImageSource = (string)newValue;
    });
    public string PlayerBG
    {
        get => GetValue(PlayerImageProperty) as string;
        set => SetValue(PlayerImageProperty, value);
    }
    public PlayerSeat()
    {
        InitializeComponent();
    }
    public String name = "";
    private void Dice_Clicked(object sender, EventArgs e)
    {
        OnDiceClicked?.Invoke(name);
    }

    internal void AnimateDice()
    {
        DiceLayer.Source = "dice_a.gif";
        DiceLayer.IsAnimationPlaying = true;
    }

    internal void StopDice(int DiceValue)
    {
        DiceLayer.Source = "dice_" + DiceValue + ".png";
        DiceLayer.IsAnimationPlaying = false;
    }

    internal void reset()
    {//HARIS FIX THIS THE SOURCE FILE NEEDS TO BE MATCHED WITH dice_0.png only then we have to reset //0001
        if (DiceLayer.Source + "" != "dice_0.png")
            DiceLayer.Source = "dice_0.png";
    }
}