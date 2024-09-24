using CommunityToolkit.Maui.Views;

namespace LudoClient;

public partial class HeaderCV : ContentView
{
    public HeaderCV()
    {
        InitializeComponent();
    }
    private void Navigate_Settings(object sender, EventArgs e)
    {
        Application.Current.MainPage.ShowPopup(new Settings());
    }
}