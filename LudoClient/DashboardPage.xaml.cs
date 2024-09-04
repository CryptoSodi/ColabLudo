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
    private void CashGame_Clicked(object sender, EventArgs e)
    {
         Navigation.PushAsync(new GameSettings());
    }
    private void Offline_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new GameSettingsLarge());
    }
    private void PlayWithFriend_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new GameSettingsLarge());
    }
    private void Practice_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new GameSettingsLarge());
    }
    private void Tournament_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new GameSettingsLarge());
    }
}