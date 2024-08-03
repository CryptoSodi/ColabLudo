namespace LudoClient.ControlView;

public partial class Token : ContentView
{
    public delegate void PieceClickedHandler(String PieceName);
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
    public String name = "";
    public String color = "Red";
    public int location = 0;
    public Token()
	{
		InitializeComponent();
	}
    private void Piece_Clicked(object sender, EventArgs e)
    {
        OnPieceClicked?.Invoke(name);
    }
}