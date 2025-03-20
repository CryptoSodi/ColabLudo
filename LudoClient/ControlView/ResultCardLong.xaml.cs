namespace LudoClient.ControlView;

public partial class ResultCardLong : ContentView
{
    public ResultCardLong()
    {
        InitializeComponent();
    }

    internal void hide()
    {
        this.IsVisible = false;
    }

    internal void init(string? playerName, string? playerPicture, string Amount, string Position)
    {
        this.IsVisible = true;
        // BackGroundImage = "user_main_bg_gold.png" BorderImage = "gold_border.png" StarImage = "star_gold.png" PlayerName = "Tassaduq"
        if (Position.Contains("*"))
        {
            BgImageItem.Source = "user_main_bg_gold.png";
            StarTypeItem.Source = "star_gold.png";
            BorderImageItem.Source = "gold_border.png";
        }
        else
        {
            BgImageItem.Source = "user_main_bg.png";
            StarTypeItem.Source = "star_silver.png";
            BorderImageItem.Source = "silver_border.png";
        }

        ChipCountHolder.IsVisible = !((Amount == "+0")|| (Amount == "-0"));
        
        
        PlayerNameItem.Text = playerName;
        PlayerImageItem.Source = playerPicture;
        StarNumberItem.Text = Position.Replace("*","");
        ChipCountItem.Text = Amount;
    }
}