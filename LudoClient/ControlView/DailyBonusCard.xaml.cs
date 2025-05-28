namespace LudoClient.ControlView;

public partial class DailyBonusCard : ContentView
{
    public DailyBonusCard()
    {
        InitializeComponent();
    }
    /// <summary>
    /// Updates the card UI to reflect claimed/unclaimed state.
    /// </summary>
    public void init(string day, string state, int Bonus)
    {
        switch (state)
        {
            case "Claimed":
                BGCard.Source = "days_current_bg.png";
                BGColor.BackgroundColor = Colors.Green; 
                break;
            case "InActive":
                BGCard.Source = "days_current_bg.png";
                BGColor.BackgroundColor = Colors.White;
                break;
            case "Active":
                BGCard.Source = "days_gold_bg.png";
                BGColor.BackgroundColor = Colors.Goldenrod;
                break;
            case "Missed":
                BGCard.Source = "days_gray_bg.png";
                BGColor.BackgroundColor = Colors.Gray;
                break;
        }
        // Assuming the first child of the Grid is the BoxView background.
        DayText.Text = day;
        BonusText.Text = Bonus.ToString();
    }
}