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
        
        // Safety: ensure no crash if list is null or empty
        if (seats == null || seats.Count == 0)
        {
            NoWinnerLabel.IsVisible = true;
            player1.hide(); player2.hide(); player3.hide(); player4.hide();
            return;
        }
        NoWinnerLabel.IsVisible = false;

        switch (GameType)
        {
            case "22":
                SafeInit(player1, seats, 0, "+" + (Double.Parse(GameCost) * 2), "1*");
                SafeInit(player2, seats, 1, "+" + (Double.Parse(GameCost) * 2), "2*");
                SafeInit(player3, seats, 2, "-" + GameCost, "3");
                SafeInit(player4, seats, 3, "-" + GameCost, "4");
                break;
            case "2":
                SafeInit(player1, seats, 0, "+" + (Double.Parse(GameCost) * 2), "1*");
                SafeInit(player2, seats, 1, "-" + GameCost, "2");
                player3.hide();
                player4.hide();
                break;
            case "3":
                SafeInit(player1, seats, 0, "+" + (Double.Parse(GameCost) * 3), "1*");
                SafeInit(player2, seats, 1, "-" + GameCost, "2");
                SafeInit(player3, seats, 2, "-" + GameCost, "3");
                player4.hide();
                break;
            case "4":
                SafeInit(player1, seats, 0, "+" + (Double.Parse(GameCost) * 4), "1*");
                SafeInit(player2, seats, 1, "-" + GameCost, "2");
                SafeInit(player3, seats, 2, "-" + GameCost, "3");
                SafeInit(player4, seats, 3, "-" + GameCost, "4");
                break;
        }
    }

    private void SafeInit(ControlView.ResultCardLong card, List<PlayerDto> seats, int index, string prize, string rank)
    {
        if (seats != null && index < seats.Count)
        {
            card.init(seats[index].PlayerName, seats[index].PlayerPicture, prize, rank);
        }
        else
        {
            card.hide();
        }
    }

    internal void init(TournamentResultDTO tournamentResultDTO)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("tak");
        
        if (tournamentResultDTO == null || tournamentResultDTO.Seats == null || tournamentResultDTO.Seats.Count == 0)
        {
            NoWinnerLabel.IsVisible = true;
            player1.hide(); player2.hide(); player3.hide(); player4.hide();
            return;
        }
        NoWinnerLabel.IsVisible = false;

        switch (tournamentResultDTO.GameType)
        {
            case "22":                
                SafeInit(player1, tournamentResultDTO.Seats, 0, "+" + ClientGlobalConstants.NormalizeCoins(tournamentResultDTO.Prize1), "1*");
                SafeInit(player2, tournamentResultDTO.Seats, 1, "+" + ClientGlobalConstants.NormalizeCoins(tournamentResultDTO.Prize2), "2*");
                SafeInit(player3, tournamentResultDTO.Seats, 2, "-" + ClientGlobalConstants.NormalizeCoins(tournamentResultDTO.Prize3), "3");
                player4.hide();
                break;
            case "2":
                SafeInit(player1, tournamentResultDTO.Seats, 0, "+" + ClientGlobalConstants.NormalizeCoins(tournamentResultDTO.Prize1), "1*");
                SafeInit(player2, tournamentResultDTO.Seats, 1, "+" + ClientGlobalConstants.NormalizeCoins(tournamentResultDTO.Prize2), "2*");
                player3.hide();
                player4.hide();
                break;
            case "3":
                SafeInit(player1, tournamentResultDTO.Seats, 0, "+" + ClientGlobalConstants.NormalizeCoins(tournamentResultDTO.Prize1), "1*");
                SafeInit(player2, tournamentResultDTO.Seats, 1, "+" + ClientGlobalConstants.NormalizeCoins(tournamentResultDTO.Prize2), "2*");
                SafeInit(player3, tournamentResultDTO.Seats, 2, "+" + ClientGlobalConstants.NormalizeCoins(tournamentResultDTO.Prize3), "3");
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