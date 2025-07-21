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
        // BackGroundImage = "user_main_bg_gold.webp" BorderImage = "gold_border.png" StarImage = "star_gold.png" PlayerName = "Tassaduq"
        if (Position.Contains("*"))
        {
            BgImageItem.ImageSource = "user_main_bg_gold.webp";
            StarTypeItem.Source = "star_gold.webp";
            BorderImageItem.Source = "gold_border.webp";
        }
        else
        {
            BgImageItem.ImageSource = "user_main_bg.webp";
            StarTypeItem.Source = "star_silver.webp";
            BorderImageItem.Source = "silver_border.webp";
        }

        ChipCountHolder.IsVisible = !((Amount == "+0")|| (Amount == "-0"));
        
        PlayerNameItem.Text = playerName;
        PlayerImageItem.Source = playerPicture;
        StarNumberItem.Text = Position.Replace("*","");
        ChipCountItem.Text = Amount;
    }
}