namespace LudoClient;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        SoundSwitch.init();
        MusicSwitch.init();
        NotificationSwitch.init();
    }
}