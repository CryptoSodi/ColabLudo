using LudoClient.Constants;

namespace LudoClient.ControlView;

public partial class Token : ContentView
{
    public String name = "";

    public delegate void PieceClickedHandler(String PieceName, bool SendToServer);
    public event PieceClickedHandler OnPieceClicked;

    public BindableProperty PlayerImageProperty = BindableProperty.Create(nameof(piece), typeof(string), typeof(PlayerSeat), propertyChanged: (bindable, oldValue, newValue) =>
    {
        var control = (Token)bindable;
        control.Piece.Source = (string)newValue;
    });
    public string piece
    {
        get => GetValue(PlayerImageProperty) as string;
        set => SetValue(PlayerImageProperty, value);
    }
    public Token()
    {
        InitializeComponent(); 
        TokenIndicatorglow.IsVisible = false;
        TokenIndicator.IsVisible = false;
        _ = StartRotatingIndicatorAsync();
    }
    public async Task StartRotatingIndicatorAsync(uint rotateDuration = 1300, double bounceScale = 1.2, uint bounceDuration = 500)
    {
        // Use a while loop to continue the rotation
        while (true)
        {
            if (TokenIndicator.IsVisible)
            {
                await Task.WhenAll(
                // Begin rotating the indicator 360 degrees continuously.
                TokenIndicator.RotateTo(360, rotateDuration, Easing.Linear)
                // Begin bouncing: scale up then back to normal.
                , BounceAsync(TokenIndicator, bounceScale, bounceDuration, false)
                // Begin bouncing: scale up then back to normal.
                , BounceAsync(TokenIndicatorglow, 1.5, bounceDuration));
            }
            else
                await Task.Delay(100);
            // Reset rotation for the next loop.
            TokenIndicator.Rotation = 0;
        }
    }
    private async Task BounceAsync(Image indicator, double bounceScale, uint duration, bool fadeFlag = true)
    {
        if (fadeFlag)
        {
            // Animate scaling up and fading out concurrently.
            await Task.WhenAll(
                indicator.ScaleTo(bounceScale, duration, Easing.CubicInOut),
                indicator.FadeTo(0.1, duration, Easing.CubicInOut)
            );

            // Animate scaling back and fading in concurrently.
            await Task.WhenAll(
                indicator.ScaleTo(1.0, duration, Easing.CubicInOut),
                indicator.FadeTo(0.6, duration, Easing.CubicInOut)
            );
        }
        else
        {
            // Only perform scaling animations when fading is disabled.
            await indicator.ScaleTo(bounceScale, duration, Easing.CubicInOut);
            await indicator.ScaleTo(1.0, duration, Easing.CubicInOut);
        }
    }

    public void ShowHideIndicator(bool flag)
    {
        TokenIndicatorglow.IsVisible = flag;
        TokenIndicator.IsVisible = flag;
    }
    private void Piece_Clicked(object sender, EventArgs e)
    {
        if ((ClientGlobalConstants.game.engine.EngineHelper.gameMode == "Computer" || ClientGlobalConstants.game.engine.EngineHelper.gameMode == "Client") && ClientGlobalConstants.game.playerColor.ToLower().Contains(name.Replace("1", "").Replace("2", "").Replace("3", "").Replace("4", "")))
            OnPieceClicked?.Invoke(name, true);
        else
            if (ClientGlobalConstants.game.engine.EngineHelper.gameMode != "Computer" && ClientGlobalConstants.game.engine.EngineHelper.gameMode != "Client")
            OnPieceClicked?.Invoke(name, true);
    }
}