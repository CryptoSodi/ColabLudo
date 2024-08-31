namespace LudoClient;

using LudoClient.Network;
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
         Navigation.PushAsync(new GameSettings());
    }
}