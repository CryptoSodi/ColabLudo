namespace LudoClient.ControlView;

public partial class Token : ContentView
{
    public String name = "";

    public delegate void PieceClickedHandler(String PieceName,bool SendToServer);
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
        OnPieceClicked?.Invoke(name,true);
    }
}