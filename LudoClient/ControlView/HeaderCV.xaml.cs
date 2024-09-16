namespace LudoClient;

public partial class HeaderCV : ContentView
{
    public HeaderCV()
    {
        InitializeComponent();
    }
    private void Navigate_Settings(object sender, EventArgs e)
    {
        Navigation.PushAsync(new SettingsPage());
    }
}