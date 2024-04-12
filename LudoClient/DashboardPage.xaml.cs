namespace LudoClient;
using Microsoft.Maui.Controls;
using System.Diagnostics;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();

    }

    private void ImageButton_Clicked(object sender, EventArgs e)
    {
         Navigation.PushAsync(new GameRoom());
    }
}
