using LudoClient.Constants;

namespace LudoClient.ControlView;

public partial class Token : ContentView
{
    public String name = "";
    public String ImageContainer = "";
    public delegate void PieceClickedHandler(String PieceName, bool SendToServer);
    public event PieceClickedHandler OnPieceClicked;

    public BindableProperty PlayerImageProperty = BindableProperty.Create(nameof(piece), typeof(string), typeof(PlayerSeat), propertyChanged: (bindable, oldValue, newValue) =>
    {
        var control = (Token)bindable;
        String image = (string)newValue;
        if (image == "ludoLockHome.png")//ludoLockHome.png
        {
            control.Piece1.Source = image;
            control.Piece1.IsVisible = true;
            control.Piece2.IsVisible = false;
            control.Piece3.IsVisible = false;
            control.Piece4.IsVisible = false;
        }
        string colorKey = image.Substring(0, 3).ToLower();
        switch (colorKey)
        {
            case "red":
                AssignImages(control,"red");
                break;
            case "gre":
                AssignImages(control, "green");
                break;
            case "yel":
                AssignImages(control, "yellow");
                break;
            case "blu":
                AssignImages(control, "blue");
                break;
        }
        control.ForceLayout();
    });

    public void UpdateView(string image)
    {
        if (ImageContainer != image)
        {
            Piece1.IsVisible = false;
            Piece2.IsVisible = false;
            Piece3.IsVisible = false;
            Piece4.IsVisible = false;
            ImageContainer = image;
            if (image.Contains("_4"))
            {
                if (!Piece4.IsVisible)
                {
                    Piece4.IsVisible = true;
                }
            }
            else
            if (image.Contains("_3"))
            {
                if (!Piece3.IsVisible)
                {
                    Piece3.IsVisible = true;                 
                }
            }
            else
            if (image.Contains("_2"))
            {
                if (!Piece2.IsVisible)
                {
                    Piece2.IsVisible = true;
                }
            }
            else
            {
                if (!Piece1.IsVisible)
                {
                    Piece1.IsVisible = true;
                }
            }
        }
    }

    private static void AssignImages(Token control, string Suffics)
    {
        if (control.Piece1.Source + "" != Suffics + "_token_large.png")
            control.Piece1.Source = Suffics + "_token_large.png";
        if (control.Piece2.Source + "" != Suffics + "_token_large_2.png")
            control.Piece2.Source = Suffics + "_token_large_2.png";
        if (control.Piece3.Source + "" != Suffics + "_token_large_3.png")
            control.Piece3.Source = Suffics + "_token_large_3.png";
        if (control.Piece4.Source + "" != Suffics + "_token_large_4.png")
            control.Piece4.Source = Suffics + "_token_large_4.png";
    }

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
        Piece1.IsVisible = false;
        Piece2.IsVisible = false;
        Piece3.IsVisible = false;
        Piece4.IsVisible = false;
        _ = StartRotatingIndicatorAsync();
    }
    public async Task StartRotatingIndicatorAsync(uint rotateDuration = 1300, double bounceScale = 1.2, uint bounceDuration = 500)
    {
        // Use a while loop to continue the rotation
        while (true)
        {
            if (piece == "ludoLockHome.png" && TokenBase.IsVisible)
            {
                TokenBase.IsVisible = false;
            }
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