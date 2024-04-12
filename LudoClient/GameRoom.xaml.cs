namespace LudoClient;

public partial class GameRoom : ContentPage
{
    int GameType = 2;
    public GameRoom()
	{
		InitializeComponent();
        NavigationPage.SetHasBackButton(this, false);
        switch (GameType)
        {
            case 2:
                Grid.SetRow(player1, 3);
                Grid.SetColumn(player1, 2);
                Grid.SetRow(player2, 5);
                Grid.SetColumn(player2, 2);
                grid.Children.Remove(player3);
                grid.Children.Remove(player4);
                break;
            case 3:
                Grid.SetRow(player1, 3);
                Grid.SetColumn(player1, 2);
                Grid.SetRow(player2, 5);
                Grid.SetColumn(player2, 1);
                Grid.SetRow(player3, 5);
                Grid.SetColumn(player3, 3);
                grid.Children.Remove(player4);
                break;
            case 4:
                Grid.SetRow(player1, 3);
                Grid.SetColumn(player1, 2);
                Grid.SetRow(player2, 4);
                Grid.SetColumn(player2, 1);
                Grid.SetRow(player3, 4);
                Grid.SetColumn(player3, 3);
                Grid.SetRow(player4, 5);
                Grid.SetColumn(player4, 2);
                break;
        }
    }
    protected override bool OnBackButtonPressed()
    {
        // Prevent back navigation
        return true;
    }
}