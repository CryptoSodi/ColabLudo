using Microsoft.Maui.Controls;

namespace LudoClient.ControlView;

public partial class PlayerSeat : ContentView
{
    public bool autoPlayFlag = false;
    public bool IsRendered { get; private set; } = false;
    private String seatColor = "";
    public String PlayerName { get; set; }
    public delegate void DiceClickedHandler(string SeatName);
    public event DiceClickedHandler OnDiceClicked;

    public delegate void TimerTimeoutHandler(string SeatName);
    public event TimerTimeoutHandler TimerTimeout;

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
    public PlayerSeat(string seatColor)
    {
        this.seatColor = seatColor;
        InitializeComponent();
        this.Loaded += OnLoaded;
            CheckBox.Source = "checkbox_"+seatColor+".png"; 
    }
    private void OnLoaded(object sender, EventArgs e)
    {
        IsRendered = true; // Mark as rendered once layout completes
        this.Loaded -= OnLoaded; // Unsubscribe to avoid repeated events
    }
    private void AutoClicked(object sender, EventArgs e)
    {         
        autoPlayFlag = !autoPlayFlag;
        if(autoPlayFlag)
            CheckBox.Source = "checkbox_"+seatColor+"_select.png";
        else
            CheckBox.Source = "checkbox_"+seatColor+".png";
    }
    private CancellationTokenSource _animationCancellationTokenSource;
    public async void StartProgressAnimation()
    {
        // Wait until the component has rendered
        while (!IsRendered)
        {
            await Task.Delay(10); // Small delay to prevent blocking
        }
        // Cancel any previous animation
        StopProgressAnimation();
        _animationCancellationTokenSource = new CancellationTokenSource();
        await AnimateProgress(_animationCancellationTokenSource.Token);
    }
    public void StopProgressAnimation()
    {
        if (_animationCancellationTokenSource != null)
        {
            ProgressBox.WidthRequest = 0; // Start with 0 width
            _animationCancellationTokenSource.Cancel();
            _animationCancellationTokenSource.Dispose();
            _animationCancellationTokenSource = null;
        }
    }
    private async Task AnimateProgress(CancellationToken token)
    {
        double totalWidth = ProgressBoxParent.Width; // Get the width of the container
        double duration = 10000; // 10 seconds in milliseconds
        double interval = 20; // Update every 20 milliseconds
        double steps = duration / interval; // Number of steps for the animation
        double widthChange = totalWidth / steps; // Width increment per step

        ProgressBox.WidthRequest = 0; // Start with 0 width
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            for (int i = 0; i <= steps; i++)
            {
                // Check if cancellation has been requested
                if (token.IsCancellationRequested)
                    return;
                if (autoPlayFlag && i > 25)
                    break;
                ProgressBox.WidthRequest = i * widthChange;
                await Task.Delay((int)interval);
            }
        }
        catch (Exception)
        {
        }
        TimerTimeout?.Invoke(seatColor);
    }
    private void Dice_Clicked(object sender, EventArgs e)
    {
        OnDiceClicked?.Invoke(seatColor);
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
    {
        Console.WriteLine("RESET DICE");
        //HARIS FIX THIS THE SOURCE FILE NEEDS TO BE MATCHED WITH dice_0.png only then we have to reset //0001
        if (DiceLayer.Source + "" != "dice_0.png")
        {
            DiceLayer.Source = "dice_0.png";
        }
    }
}