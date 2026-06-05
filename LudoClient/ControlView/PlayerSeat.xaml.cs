using LudoClient.Constants;
using SharedCode.CoreEngine;

namespace LudoClient.ControlView;
    public partial class PlayerSeat : ContentView
    {
        // Static cache to pre-load dice images and prevent disk lag during gameplay
        private static readonly Dictionary<int, ImageSource> DiceCache = new()
        {
            { 1, ImageSource.FromFile("dice_1.webp") },
            { 2, ImageSource.FromFile("dice_2.webp") },
            { 3, ImageSource.FromFile("dice_3.webp") },
            { 4, ImageSource.FromFile("dice_4.webp") },
            { 5, ImageSource.FromFile("dice_5.webp") },
            { 6, ImageSource.FromFile("dice_6.webp") }
        };

    public bool autoPlayFlag { get; set; }
    public bool isAutoPlayDisabled = false;//Set it to true to prevent Auto Play of other players on localclient
    public String seatColor { get; set; }
    public String PlayerName { get; set; }
    public String PictureUrl { get; set; }
    public EngineHelper engineHelper { get; internal set; }
    public bool IsRendered { get; private set; } = false;    
    private bool _micEnabled = true;
    private bool _speakerEnabled = true;
    private bool _cameraEnabled = true;

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
        UpdateMediaButtonSources();
        UpdateMicVisibility();
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
        UpdateMicVisibility();
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
    private void MicClicked(object sender, EventArgs e)
    {
        _micEnabled = !_micEnabled;
        CheckBoxMic.Source = GetMediaButtonSource(_micEnabled, "M");
    }
    private void SpeakerClicked(object sender, EventArgs e)
    {
        _speakerEnabled = !_speakerEnabled;
        CheckBoxSpeaker.Source = GetMediaButtonSource(_speakerEnabled, "S");
    }
    private void CameraClicked(object sender, EventArgs e)
    {
        _cameraEnabled = !_cameraEnabled;
        CheckBoxCamera.Source = GetMediaButtonSource(_cameraEnabled, "C");
    }
    private void UpdateMediaButtonSources()
    {
        CheckBoxMic.Source = GetMediaButtonSource(_micEnabled, "M");
        CheckBoxSpeaker.Source = GetMediaButtonSource(_speakerEnabled, "S");
        CheckBoxCamera.Source = GetMediaButtonSource(_cameraEnabled, "C");
    }
    private void UpdateMicVisibility()
    {
        var ownColor = ClientGlobalConstants.game?.playerColor;
        CheckBoxMic.IsVisible = !string.IsNullOrWhiteSpace(ownColor) &&
            string.Equals(ownColor, seatColor, StringComparison.OrdinalIgnoreCase);
    }
    private string GetMediaButtonSource(bool isEnabled, string suffix)
    {
        var colorPrefix = string.IsNullOrWhiteSpace(seatColor)
            ? "R"
            : seatColor.Substring(0, 1).ToUpperInvariant();
        var statePrefix = isEnabled ? colorPrefix : "D";
        return $"{statePrefix}RB{suffix}.webp";
    }
    public void AttachCameraView(View? cameraView, bool isVisible)
    {
        if (cameraView == null)
            return;

        if (cameraView.Parent is Layout oldParent && oldParent != CameraHost)
            oldParent.Children.Remove(cameraView);

        if (isVisible)
        {
            if (!CameraHost.Children.Contains(cameraView))
                CameraHost.Children.Add(cameraView);

            cameraView.IsVisible = true;
            cameraView.HorizontalOptions = LayoutOptions.Fill;
            cameraView.VerticalOptions = LayoutOptions.Fill;
            cameraView.Rotation = 0;
            cameraView.Margin = 0;
        }
        else
        {
            if (CameraHost.Children.Contains(cameraView))
                CameraHost.Children.Remove(cameraView);

            cameraView.IsVisible = false;
        }
    }
    private CancellationTokenSource _animationCancellationTokenSource;
    public void StartProgressAnimation()
    {
        // Cancel any previous animation
        StopProgressAnimation();
        
        ProgressBox.WidthRequest = 0;

        // Animate from 0.0 (0%) to 1.0 (100%)
        // This makes the logic independent of when the layout calculates the actual Width
        var animation = new Animation(v => 
        {
            if (ProgressBoxParent.Width > 0)
                ProgressBox.WidthRequest = v * ProgressBoxParent.Width;
        }, 0, 1);
        
        animation.Commit(this, "ProgressAnimation", 16, 10000, Easing.Linear, (finalValue, wasCancelled) =>
        {
            // wasCancelled is false if the animation reached 10 seconds naturally
            if (!wasCancelled)
            {
                MainThread.BeginInvokeOnMainThread(async () => {
                    // Trigger the move logic (ExecuteAutoPlayLogic handles its own internal safety checks)
                    await ExecuteAutoPlayLogic();
                });
            }
        });

        // Background monitor for proactive auto-play (User specifically clicked 'Auto')
        _animationCancellationTokenSource = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            var token = _animationCancellationTokenSource.Token;
            await Task.Delay(1000); 

            while (!token.IsCancellationRequested)
            {
                if (autoPlayFlag && !engineHelper.animationBlock)
                {
                    bool canAct = engineHelper.gameMode != "Client" || 
                                 (ClientGlobalConstants.game != null && !ClientGlobalConstants.game.engine.processing && !ClientGlobalConstants.game.isInputLocked);

                    if (canAct)
                    {
                        MainThread.BeginInvokeOnMainThread(async () => {
                            StopProgressAnimation();
                            await ExecuteAutoPlayLogic();
                        });
                        break;
                    }
                }
                await Task.Delay(100);
            }
        }, _animationCancellationTokenSource.Token);
    }

    public void StopProgressAnimation()
    {
        this.AbortAnimation("ProgressAnimation");
        if (_animationCancellationTokenSource != null)
        {
            _animationCancellationTokenSource.Cancel();
            _animationCancellationTokenSource.Dispose();
            _animationCancellationTokenSource = null;
        }
        ProgressBox.WidthRequest = 0;
    }

    private async Task AnimateProgress(CancellationToken token)
    {
        // Method body removed as logic moved to StartProgressAnimation for ScaleXTo implementation
        await Task.CompletedTask;
    }
    private async Task ExecuteAutoPlayLogic()
    {
        if(!isAutoPlayDisabled && ClientGlobalConstants.game != null)
            if (engineHelper.checkTurn(engineHelper.currentPlayer.Color, "RollDice"))
            {
                Console.WriteLine("Client AI Requesting Dice Roll");

                await ClientGlobalConstants.game.PlayerDiceClicked(seatColor, "", "", "", engineHelper.gameMode == "Client");
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
            if (DiceCache.TryGetValue(DiceValue, out var cachedSource))
            {
                DiceLayer.Source = cachedSource;
            }
            else
            {
                DiceLayer.Source = $"dice_{DiceValue}.webp";
            }
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
