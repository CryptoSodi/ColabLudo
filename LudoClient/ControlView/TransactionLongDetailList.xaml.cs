namespace LudoClient.ControlView;

public partial class TransactionLongDetailList : ContentView
{
    public TransactionLongDetailList()
    {
        InitializeComponent();
    }

    private void Expand_Clicked(object sender, EventArgs e)
    {
        if (ExpandSheet.Padding.Top > 0)
        {
            ExpandSheet.Padding = new Thickness(0, 0, 0, 0);
            ExpandSheet.Margin = new Thickness(0, 0, 0, 0);
            SheetDirection.Source = "arr_down.png";
            return;
        }
        else
        {
            SheetDirection.Source = "arr_up.png";
            ExpandSheet.Padding = new Thickness(0, (SubSheet.Height - 10), 0, 0);
        }
    }
}