using SharedCode.CoreEngine;

namespace SharedCode.ControlView;
public partial class PlayerSeat : ContentView
{
    public bool autoPlayFlag { get; set; }
    public bool isAutoPlayDisabled = false;//Set it to true to prevent Auto Play of other players on localclient
    public String seatColor { get; set; }
    public String PlayerName { get; set; }
    public String PlayerImageSource { get; set; }
    public EngineHelper engineHelper { get; internal set; }
    public bool IsRendered { get; private set; } = false;

    MainPage game { get; set; }

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

    public void initAuto(MainPage game, String PlayerName, String PictureUrl, string checkBoxFlag="Show", bool autoPlayFlag = true, bool isAutoPlayDisabled = false)
    {
        this.game = game;
        this.PlayerName = PlayerName;
        PlayerNameText.Text = this.PlayerName;
        PlayerImageSource = PictureUrl;
        PlayerImage.Source = PlayerImageSource;

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
                if (autoPlayFlag && i > 2 && !engineHelper.animationBlock)
                {
                    break;
                }   
                ProgressBox.WidthRequest = i * widthChange;
                await Task.Delay((int)interval);
            }
        }
        catch (Exception)
        {
        }
            await ExecuteAutoPlayLogic();
    }
    private async Task ExecuteAutoPlayLogic()
    {
        if (!isAutoPlayDisabled && game != null)
            if (engineHelper.checkTurn(engineHelper.currentPlayer.Color, "RollDice"))
            {
                Console.WriteLine("Client AI Requesting Dice Roll");
                game.PlayerDiceClicked(seatColor, "", "", "", engineHelper.gameMode == "Client");
            }
            else
            {
                string result1 = engineHelper.AIRequestPiece(engineHelper.currentPlayer.Color);
                string piece1String = result1.Split(",")[0];
                string piece2String = result1.Split(",")[1];

                await game.MovePiece(piece1String, piece2String, engineHelper.gameMode == "Client");
            }
    }
    private void Dice_Clicked(object sender, EventArgs e)
    {
        //StartProgressAnimation();

        foreach (var piece in engineHelper.currentPlayer.Pieces)
        {
            piece.Moveable = false;
            piece.DoubleMoveable = false;

            if (piece.Location == 0 && engineHelper.diceValue == 6)// Open the token if it's in base and dice shows a 6
                piece.Moveable = true;
            else if (piece.Location != 0)
            {
                //The piece is moveable now decide if it can only move alone or double movement is also allowed
                if ((piece.Location + engineHelper.diceValue <= 51 && !engineHelper.currentPlayer.CanEnterGoal) || (piece.Location + engineHelper.diceValue <= 57 && engineHelper.currentPlayer.CanEnterGoal))
                {
                    //Check if the piece is not in the home zone and can move to the home zone zlone
                    bool pathBlocked = false;

                    var Stepperpiece = piece.Clone();
                    for (int step = 1; step < engineHelper.diceValue; step++)
                    {
                        Stepperpiece.Jump(game.engine, 1, true);

                        string newBox = Stepperpiece.getPieceBox();
                        List<Piece> tokensAtIntermediate = game.engine.board?[newBox].Where(p => p.Color != piece.Color && !(engineHelper.gameType == "22" && engineHelper.IsTeammate(piece.Color, p.Color))).ToList();

                        if (tokensAtIntermediate?.Count > 1 && !engineHelper.safeZone.Contains(Stepperpiece.Position))
                        {
                            pathBlocked = true;
                            break;
                        }
                    }
                    piece.Moveable = !pathBlocked;
                }

                if (engineHelper.diceValue == 2 || engineHelper.diceValue == 4 || engineHelper.diceValue == 6)
                {
                    // New logic to handle double token jump over a block
                    if (piece.Location <= 51)
                    {
                        // Check if another token is on the same position
                        var samePositionTokens = engineHelper.currentPlayer.Pieces
                            .Where(p => p.getPieceBox() == piece.getPieceBox())
                            .ToList();

                        if (samePositionTokens.Count > 1 && (piece.Location + (engineHelper.diceValue / 2) <= 51))
                        {   //Double Move is not allowed in the Home Zone
                            //Allow both tokens to move together
                            piece.DoubleMoveable = true;
                        }
                    }
                }
            }
        }

        List<Piece> moveablePieces = engineHelper.currentPlayer.Pieces.Where(p => p.Moveable).ToList();
        List<List<Piece>> DoubleMoveablePieces = engineHelper.currentPlayer.Pieces.Where(p => p.DoubleMoveable).GroupBy(p => p.getPieceBox()).Where(g => g.Count() > 1).Select(g => g.ToList()).ToList(); // This is List<List<Piece>>

        Console.WriteLine($"{engineHelper.index} : {engineHelper.currentPlayer.Color} rolled a {engineHelper.diceValue}. Can move {moveablePieces.Count} double move: {DoubleMoveablePieces.Count} pieces. ");

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