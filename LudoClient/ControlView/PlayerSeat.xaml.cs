using LudoClient.Constants;
using SharedCode.CoreEngine;

namespace LudoClient.ControlView;
public partial class PlayerSeat : ContentView
{
    public bool autoPlayFlag = false;
    public String seatColor = "";
    public String PlayerName = "";
    public String PlayerImageSource = "";
    public EngineHelper EngineHelper { get; internal set; }
    public bool IsRendered { get; private set; } = false;

    public delegate void DiceClickedHandler(string SeatName, String DiceValue, String Piece1, String Piece2, bool SendToServer = true);
    public event DiceClickedHandler OnDiceClicked;

    public delegate Task<string> TimerTimeoutHandler(string SeatName);
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

    public void showAuto(String PlayerName, String PictureUrl, bool hideAll, bool autoPlayFlag)
    {
        this.PlayerName = PlayerName;
        PlayerImageSource = PictureUrl;

        PlayerImage.Source = PictureUrl;
        PlayerNameText.Text = PlayerName;
        this.autoPlayFlag = autoPlayFlag;
        Grid.SetColumn(ProgressBoxParent, 1);
        Grid.SetColumnSpan(ProgressBoxParent, 1);
        CheckBox.IsVisible = true;
        ProgressBoxText.IsVisible = true;
        ProgressBoxParentContainer.IsVisible = true;
    }
    public void hideAuto(String PlayerName, String PictureUrl, bool hideAll, bool autoPlayFlag)
    {
        this.PlayerName = PlayerName;
        PlayerImageSource = PictureUrl;

        PlayerImage.Source = PictureUrl;
        PlayerNameText.Text = PlayerName;
        this.autoPlayFlag = autoPlayFlag;
        ProgressBoxParentContainer.IsVisible = false;
        Grid.SetColumn(ProgressBoxParent, 0);
        Grid.SetColumnSpan(ProgressBoxParent, 2);
        CheckBox.IsVisible = false;
        ProgressBoxText.IsVisible = false;
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
        if (!CheckBox.IsVisible)
            return;
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
            await Task.Delay(10); // Small delay to prevent blocking
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
        if (EngineHelper.stopAnimate)
        {
            await Task.Delay(400);
            TimerTimeout?.Invoke(seatColor);
            return;
        }
        ProgressBox.WidthRequest = 0; // Start with 0 width

        try
        {
            for (int i = 0; i <= steps; i++)
            {
                // Check if cancellation has been requested
                if (token.IsCancellationRequested)
                    return;
                if (autoPlayFlag && i > 50 && !EngineHelper.animationBlock)
                    break;
                ProgressBox.WidthRequest = i * widthChange;
                await Task.Delay((int)interval);
            }
        }
        catch (Exception)
        {
        }
        if(EngineHelper.gameMode != "Client")
            TimerTimeout?.Invoke(seatColor);
    }
    private void Dice_Clicked(object sender, EventArgs e)
    {
        if ((ClientGlobalConstants.game.engine.EngineHelper.gameMode == "Computer" || ClientGlobalConstants.game.engine.EngineHelper.gameMode == "Client") && ClientGlobalConstants.game.playerColor.ToLower() == seatColor)
            OnDiceClicked?.Invoke(seatColor, "", "", "");
        else
            if (ClientGlobalConstants.game.engine.EngineHelper.gameMode != "Computer" && ClientGlobalConstants.game.engine.EngineHelper.gameMode != "Client")
            OnDiceClicked?.Invoke(seatColor, "", "", "");
    }
    internal void AnimateDice()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            DiceLayer.Source = "dice_a.gif";
            DiceLayer.IsAnimationPlaying = true;
        });
    }
    internal void StopDice(int DiceValue)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Thread.Sleep(200);
            DiceLayer.IsAnimationPlaying = false;
            DiceLayer.Source = $"dice_{DiceValue}.png";
        });
    }

    internal void reset()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (DiceLayer.Source is FileImageSource fileSource &&
                fileSource.File != "dice_0.png")
            {
                // Stop any animation & reconnect fresh
                DiceLayer.IsAnimationPlaying = false;
                DiceLayer.Source = "dice_0.png";
            }
        });
    }
    internal void PlayerLeft()
    {
        reset();
        PlayerNameText.Text = "Left";
        PlayerImage.Source = "user.png";
        ProgressBoxParentContainer.IsVisible = false;
        playerBG.ImageSource = "gray_container.png";
    }
}