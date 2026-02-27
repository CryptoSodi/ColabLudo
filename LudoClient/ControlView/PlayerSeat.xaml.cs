using LudoClient.Constants;
using SharedCode.CoreEngine;

namespace LudoClient.ControlView;
public partial class PlayerSeat : ContentView
{
    public bool autoPlayFlag { get; set; }
    public bool isAutoPlayDisabled = false;//Set it to true to prevent Auto Play of other players on localclient
    public String seatColor { get; set; }
    public String PlayerName { get; set; }
    public String PictureUrl { get; set; }
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
    public PlayerSeat setColor(string seatColor)
    {
        this.seatColor = seatColor;
        CheckBox.Source = "checkbox_" + seatColor + ".webp";
        switch (seatColor.ToLower())
        {
            case "green":
                PlayerBG = "green_container.webp";
                break;
            case "yellow":
                PlayerBG = "yellow_container.webp";
                break;
            case "blue":
                PlayerBG = "blue_container.webp";
                break;
            case "red":
                PlayerBG = "red_container.webp";
                break;
        }
        return this;
    }
    public void initAuto(String PlayerName, String PictureUrl, string checkBoxFlag="Show", bool autoPlayFlag = true, bool isAutoPlayDisabled = false)
    {
        Console.WriteLine($"Initializing PlayerSeat for {PlayerName} with image {PictureUrl} and checkbox flag {checkBoxFlag}");
        this.PlayerName = PlayerName;
        PlayerNameText.Text = this.PlayerName;
        this.PictureUrl = PictureUrl;
        // Delay image loading by 1 second
        Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(1), () =>
        {
            PlayerImage.Source = this.PictureUrl;
        });

        switch (checkBoxFlag)
        {
            case "ShowAuto":
                CheckBox.IsVisible = true;
                ProgressBoxText.IsVisible = true;
                Grid.SetColumn(ProgressBoxParent, 1);
                Grid.SetColumnSpan(ProgressBoxParent, 1);
                ProgressBoxParentContainer.IsVisible = true;
                break;
            case "HideAuto":
                CheckBox.IsVisible = false;
                ProgressBoxText.IsVisible = false;
                Grid.SetColumn(ProgressBoxParent, 0);
                Grid.SetColumnSpan(ProgressBoxParent, 2);
                ProgressBoxParentContainer.IsVisible = true;
                break;
            case "HideAll":
                CheckBox.IsVisible = false;
                ProgressBoxText.IsVisible = false;
                ProgressBoxParent.IsVisible = false;
                ProgressBoxParentContainer.IsVisible = false;
                Grid.SetColumn(ProgressBoxParent, 0);
                Grid.SetColumnSpan(ProgressBoxParent, 2);
                break;
        }

        this.autoPlayFlag = autoPlayFlag;
        this.isAutoPlayDisabled = isAutoPlayDisabled;
    }
    public PlayerSeat()
    {
        InitializeComponent();
        IsRendered = true; // Mark as rendered once layout completes
    }
    private void AutoClicked(object sender, EventArgs e)
    {
        if (!CheckBox.IsVisible)
            return;
        autoPlayFlag = !autoPlayFlag;
        if(autoPlayFlag)
            CheckBox.Source = "checkbox_"+seatColor+ "_select.webp";
        else
            CheckBox.Source = "checkbox_"+seatColor+ ".webp";
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
        if(!isAutoPlayDisabled && ClientGlobalConstants.game != null)
            if (engineHelper.checkTurn(engineHelper.currentPlayer.Color, "RollDice"))
            {
                Console.WriteLine("Client AI Requesting Dice Roll");
                
                    ClientGlobalConstants.game.PlayerDiceClicked(seatColor, "", "", "", engineHelper.gameMode == "Client");
            }
            else
            {
                string result1 = engineHelper.AIRequestPiece(engineHelper.currentPlayer.Color);
                string piece1String = result1.Split(",")[0];
                string piece2String = result1.Split(",")[1];
                
                    await ClientGlobalConstants.game.MovePiece(piece1String, piece2String, engineHelper.gameMode == "Client");
            }
        await Task.Delay(500);
    }
    private void Dice_Clicked(object sender, EventArgs e)
    {
        if ((engineHelper.gameMode == "Computer" || engineHelper.gameMode == "Client") && ClientGlobalConstants.game.playerColor.ToLower() == seatColor)
        {
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
            DiceLayer.Source = $"dice_{DiceValue}.webp";
        });
    }
    internal void reset()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (DiceLayer.Source is FileImageSource fileSource &&
                fileSource.File != "dice_0.webp")
            {
                // Stop any animation & reconnect fresh
                DiceLayer.IsAnimationPlaying = false;
                DiceLayer.Source = "dice_0.webp";
            }
        });
    }
    internal void PlayerLeft()
    {
        reset();
        PlayerNameText.Text = "Left";
        PlayerImage.Source = "user.webp";
        ProgressBoxParentContainer.IsVisible = false;
        playerBG.ImageSource = "gray_container.webp";
    }
}