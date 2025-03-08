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
        player1.PlayerName = seats[0].PlayerName;
        player1.PlayerImage = seats[0].PlayerPicture;
        player2.PlayerName = seats[1].PlayerName;
        player2.PlayerImage = seats[1].PlayerPicture;
    }
    private void BtnExit(object sender, EventArgs e)
    {
        ClientGlobalConstants.GoBack();
    }
}