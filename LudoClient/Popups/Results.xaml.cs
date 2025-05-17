using LudoClient.Constants;
using SharedCode;
using SimpleToolkit.Core;

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
    internal void init(List<PlayerDto> seats, string GameType, string GameCost)
    {
        switch (GameType)
        {
            case "22":
                //BackGroundImage = "user_main_bg_gold.png" BorderImage = "gold_border.png" StarImage = "star_gold.png" PlayerName = "Tassaduq"
                player1.init(seats[0].PlayerName, seats[0].PlayerPicture, "+" + (Double.Parse(GameCost) * 2), "1*");
                player2.init(seats[1].PlayerName, seats[1].PlayerPicture, "+" + (Double.Parse(GameCost) * 2), "2*");
                player3.init(seats[3].PlayerName, seats[3].PlayerPicture, "-" + GameCost, "3");
                player4.init(seats[4].PlayerName, seats[4].PlayerPicture, "-" + GameCost, "4");
                break;
            case "2":
                player1.init(seats[0].PlayerName, seats[0].PlayerPicture, "+" + (Double.Parse(GameCost) * 2), "1*");
                player2.init(seats[1].PlayerName, seats[1].PlayerPicture, "-" + GameCost, "2");
                player3.hide();
                player4.hide();
                break;
            case "3":
                player1.init(seats[0].PlayerName, seats[0].PlayerPicture, "+" + (Double.Parse(GameCost) * 3), "1*");
                player2.init(seats[1].PlayerName, seats[1].PlayerPicture, "-" + GameCost, "2");
                player3.init(seats[2].PlayerName, seats[2].PlayerPicture, "-" + GameCost, "3");
                player4.hide();
                break;
            case "4":
                player1.init(seats[0].PlayerName, seats[0].PlayerPicture, "+" + (Double.Parse(GameCost) * 4), "1*");
                player2.init(seats[1].PlayerName, seats[1].PlayerPicture, "-" + GameCost, "2");
                player3.init(seats[2].PlayerName, seats[2].PlayerPicture, "-" + GameCost, "3");
                player4.init(seats[3].PlayerName, seats[3].PlayerPicture, "-" + GameCost, "4");
                break;
        }
    }
    private void BtnExit(object sender, EventArgs e)
    {
        ClientGlobalConstants.GoBack();
    }
}