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

        //ImageSwitch activeTab = sender as ImageSwitch;
        //TabC1.SwitchSource = TabC1 == activeTab ? TabC1.SwitchOn : TabC1.SwitchOff;
        //TabC2.SwitchSource = TabC2 == activeTab ? TabC2.SwitchOn : TabC2.SwitchOff;
        //TabC3.SwitchSource = TabC3 == activeTab ? TabC3.SwitchOn : TabC3.SwitchOff;
        //TabC4.SwitchSource = TabC4 == activeTab ? TabC4.SwitchOn : TabC4.SwitchOff;
        //// Add logic here to change the content based on the active tab
        //if (TabC1 == activeTab)
        //    playerColor = "Red";
        //if (TabC2 == activeTab)
        //    playerColor = "Green";
        //if (TabC3 == activeTab)
        //    playerColor = "Blue";
        //if (TabC4 == activeTab)
        //    playerColor = "Yellow";
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
        Console.WriteLine("ANIMATE PROGRESS");
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
        Console.WriteLine("PROGRESS STARTED");
        double totalWidth = ProgressBoxParent.Width; // Get the width of the container
        double duration = 10000; // 10 seconds in milliseconds
        double interval = 20; // Update every 20 milliseconds
        double steps = duration / interval; // Number of steps for the animation
        double widthChange = totalWidth / steps; // Width increment per step

        ProgressBox.WidthRequest = 0; // Start with 0 width
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i <= steps; i++)
        {
            // Check if cancellation has been requested
            if (token.IsCancellationRequested)
                return;
            if (autoPlayFlag && i>25)
                break;
            ProgressBox.WidthRequest = i * widthChange;
            await Task.Delay((int)interval);
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