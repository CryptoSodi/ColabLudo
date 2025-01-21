using LudoClient.Constants;
using LudoClient.ControlView;

namespace LudoClient;

public partial class PlayWithFriends : ContentPage
{
    public PlayWithFriends()
    {
        InitializeComponent();
        Tab1.SwitchSource = Tab1.SwitchOn;
        Tab2.SwitchSource = Tab2.SwitchOff;
        Tab3.SwitchSource = Tab3.SwitchOff;
        Tab4.SwitchSource = Tab4.SwitchOff;

        TabP1.SwitchSource = TabP1.SwitchOn;
        TabP2.SwitchSource = TabP2.SwitchOff;
    }
    private void TabRequestedActivate(object sender, EventArgs e)
    {
        ImageSwitch activeTab = sender as ImageSwitch;
        Tab1.SwitchSource = Tab1 == activeTab ? Tab1.SwitchOn : Tab1.SwitchOff;
        Tab2.SwitchSource = Tab2 == activeTab ? Tab2.SwitchOn : Tab2.SwitchOff;
        Tab3.SwitchSource = Tab3 == activeTab ? Tab3.SwitchOn : Tab3.SwitchOff;
        Tab4.SwitchSource = Tab4 == activeTab ? Tab4.SwitchOn : Tab4.SwitchOff;
    }
    private void TabRequestedActivateP(object sender, EventArgs e)
    {
        ImageSwitch activeTab = sender as ImageSwitch;
        TabP1.SwitchSource = TabP1 == activeTab ? TabP1.SwitchOn : TabP1.SwitchOff;
        TabP2.SwitchSource = TabP2 == activeTab ? TabP2.SwitchOn : TabP2.SwitchOff;
        if (TabP1 == activeTab)
        {
            TabJoin.IsVisible = false;
            TabCreate.IsVisible = true;
            actionBtn.ImageSource = Skins.CreateBTN; //"btn_create.png";
            //public static string CreateBTN => $"btn_create{CurrentSkinType}.png";
        }
        else
        {
            TabJoin.IsVisible = true;
            TabCreate.IsVisible = false;
            actionBtn.ImageSource = Skins.JoinBTN; //"btn_join_large.png";
            //public static string JoinBTN => $"btn_join_large{CurrentSkinType}.png";
        }
    }
}