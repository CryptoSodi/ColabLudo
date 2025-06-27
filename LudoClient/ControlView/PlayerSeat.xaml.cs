using LudoClient.Constants;
using SharedCode;
using SharedCode.Constants;
using SharedCode.CoreEngine;

namespace LudoClient.ControlView;
public partial class PlayerSeat : ContentView
{
    public bool autoPlayFlag = false;
    public String seatColor = "";
    public String PlayerName = "";
    public String PlayerImageSource = "";
    public EngineHelper engineHelper { get; internal set; }
    public bool IsRendered { get; private set; } = false;

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
        
        ProgressBox.WidthRequest = 0; // Start with 0 width

        try
        {
            for (int i = 0; i <= steps; i++)
            {
                // Check if cancellation has been requested
                if (token.IsCancellationRequested)
                    return;
                if (autoPlayFlag && i > 50 && !engineHelper.animationBlock)
                {
                    if (engineHelper.gameMode == "Client")
                        await ExecuteAutoPlayLogic();
                    break;
                }   
                ProgressBox.WidthRequest = i * widthChange;
                await Task.Delay((int)interval);
            }
        }
        catch (Exception)
        {
        }
        if(engineHelper.gameMode != "Client")
        {
            await ExecuteAutoPlayLogic();
        }
    }
    private async Task ExecuteAutoPlayLogic()
    {
        if (engineHelper.checkTurn(engineHelper.currentPlayer.Color, "RollDice"))
        {
            Console.WriteLine("Client AI Requesting Dice Roll");
            ClientGlobalConstants.game.engine.EngineHelper.indexServer++;
            ClientGlobalConstants.game.PlayerDiceClicked(seatColor, "", "", "", engineHelper.gameMode == "Client");
        }
        else
        {
            string result1 = engineHelper.AIRequestPiece(engineHelper.currentPlayer.Color);
            string piece1String = result1.Split(",")[0];
            string piece2String = result1.Split(",")[1];

            ClientGlobalConstants.game.engine.EngineHelper.indexServer++;
            await ClientGlobalConstants.game.MovePiece(piece1String, piece2String, engineHelper.gameMode == "Client");
        }

        await Task.Delay(500);
    }
    private void Dice_Clicked(object sender, EventArgs e)
    {
        if ((engineHelper.gameMode == "Computer" || engineHelper.gameMode == "Client") && ClientGlobalConstants.game.playerColor.ToLower() == seatColor)
        {
            ClientGlobalConstants.game.engine.EngineHelper.indexServer++;
            ClientGlobalConstants.game.PlayerDiceClicked(seatColor, "", "", "", true);
        }
        else
            if (engineHelper.gameMode != "Computer" && engineHelper.gameMode != "Client")
                ClientGlobalConstants.game.PlayerDiceClicked(seatColor, "", "", "");
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