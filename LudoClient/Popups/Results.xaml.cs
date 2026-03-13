using LudoClient.Constants;
using SharedCode;
using SimpleToolkit.Core;
using System.Security.AccessControl;

namespace LudoClient.Popups;

public partial class Results : ContentPage
{
    public Results()
    {
        InitializeComponent();
        /*
		 * <Image Source="user_main_bg_gold.webp" />
                <Image Source="user_main_bg.webp" />
                <Image Source="gold_border.webp" />
                <Image Source="star_silver.webp" />
                <Image Source="star_gold.webp" />
                <Image Source="ic_chips_spades.webp" />
                */
    }
    internal void init(List<PlayerDto> seats, string GameType, string GameCost)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("tak");
        switch (GameType)
        {
            case "22":
                //BackGroundImage = "user_main_bg_gold.webp" BorderImage = "gold_border.webp" StarImage = "star_gold.webp" PlayerName = "Tassaduq"
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

    internal void init(TournamentResultDTO tournamentResultDTO)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("tak");
        switch (tournamentResultDTO.GameType)
        {
            case "22":                
                //BackGroundImage = "user_main_bg_gold.webp" BorderImage = "gold_border.webp" StarImage = "star_gold.webp" PlayerName = "Tassaduq"
                player1.init(tournamentResultDTO.Seats[0].PlayerName, tournamentResultDTO.Seats[0].PlayerPicture, "+" + ClientGlobalConstants.NormalizeCoins(tournamentResultDTO.Prize1), "1*");
                player2.init(tournamentResultDTO.Seats[1].PlayerName, tournamentResultDTO.Seats[1].PlayerPicture, "+" + ClientGlobalConstants.NormalizeCoins(tournamentResultDTO.Prize2), "2*");
                player3.init(tournamentResultDTO.Seats[2].PlayerName, tournamentResultDTO.Seats[2].PlayerPicture, "-" + ClientGlobalConstants.NormalizeCoins(tournamentResultDTO.Prize3), "3");
                player4.hide();
                break;
            case "2":
                player1.init(tournamentResultDTO.Seats[0].PlayerName, tournamentResultDTO.Seats[0].PlayerPicture, "+" + ClientGlobalConstants.NormalizeCoins(tournamentResultDTO.Prize1), "1*");
                player2.init(tournamentResultDTO.Seats[1].PlayerName, tournamentResultDTO.Seats[1].PlayerPicture, "+" + ClientGlobalConstants.NormalizeCoins(tournamentResultDTO.Prize2), "2*");
                player3.hide();
                player4.hide();
                break;
            case "3":
                player1.init(tournamentResultDTO.Seats[0].PlayerName, tournamentResultDTO.Seats[0].PlayerPicture, "+" + ClientGlobalConstants.NormalizeCoins(tournamentResultDTO.Prize1), "1*");
                player2.init(tournamentResultDTO.Seats[1].PlayerName, tournamentResultDTO.Seats[1].PlayerPicture, "+" + ClientGlobalConstants.NormalizeCoins(tournamentResultDTO.Prize2), "2*");
                player3.init(tournamentResultDTO.Seats[2].PlayerName, tournamentResultDTO.Seats[2].PlayerPicture, "+" + ClientGlobalConstants.NormalizeCoins(tournamentResultDTO.Prize3), "3");
                player4.hide();
                break;
            case "4":
                break;
        }
    }

    private void BtnExit(object sender, EventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        Thread.Sleep(50);
        ClientGlobalConstants.GoBack();
    }
}