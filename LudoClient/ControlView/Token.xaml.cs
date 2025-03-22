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