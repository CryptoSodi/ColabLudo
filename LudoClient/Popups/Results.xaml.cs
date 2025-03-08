using CommunityToolkit.Maui.Views;
using LudoClient.Constants;
using SharedCode;

namespace LudoClient.Popups;

public partial class Results : ContentPage
{
    public Results()
    {
        InitializeComponent();
        /*
		 * <Image Source="user_main_bg_gold.png" />
                <Image Source="user_main_bg.png" />
                <Image Source="gold_border.png" />
                <Image Source="star_silver.png" />
                <Image Source="star_gold.png" />
                <Image Source="ic_chips_spades.png" />
                */
    }
    internal void init(List<PlayerDto> seats)
    {
        // BackGroundImage = "user_main_bg_gold.png" BorderImage = "gold_border.png" StarImage = "star_gold.png" PlayerName = "Tassaduq"
        
        player1.init(seats[0].PlayerName, seats[0].PlayerPicture, "20", "1");
        player2.init(seats[1].PlayerName, seats[1].PlayerPicture, "20", "2");
    }
    private void BtnExit(object sender, EventArgs e)
    {
        ClientGlobalConstants.GoBack();
    }
}