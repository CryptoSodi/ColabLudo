namespace LudoClient;

public partial class WaitingRoom : ContentPage
{
    int GameType = 2;
    public WaitingRoom()
    {
        InitializeComponent();
        switch (GameType)
        {
            case 2:
                thunderAnimation.Source = "player2_00.png";
                Grid.SetRow(player1, 2);
                Grid.SetColumn(player1, 1);
                Grid.SetRow(player2, 4);
                Grid.SetColumn(player2, 3);
                grid.Children.Remove(player3);
                grid.Children.Remove(player4);
                break;
            case 3:
                thunderAnimation.Source = "player3_00.png";
                Grid.SetRow(player1, 2);
                Grid.SetColumn(player1, 2);
                Grid.SetRow(player2, 4);
                Grid.SetColumn(player2, 1);
                Grid.SetRow(player3, 4);
                Grid.SetColumn(player3, 3);
                grid.Children.Remove(player4);
                break;
            case 4:
                thunderAnimation.Source = "player4_00.png";
                Grid.SetRow(player1, 2);
                Grid.SetColumn(player1, 2);
                Grid.SetRow(player2, 3);
                Grid.SetColumn(player2, 1);
                Grid.SetRow(player3, 3);
                Grid.SetColumn(player3, 3);
                Grid.SetRow(player4, 4);
                Grid.SetColumn(player4, 2);
                break;
        }
    }
}