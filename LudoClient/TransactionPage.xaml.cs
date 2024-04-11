using LudoClient.ControlView;

namespace LudoClient;

public partial class TransactionPage : ContentPage
{
    private void TabRequestedActivate(object sender, EventArgs e)
    {
        ActivateTab(sender as ImageSwitch);
    }
    private void ActivateTab(ImageSwitch activeTab)
    {
        Tab1.SwitchSource = Tab1 == activeTab ? Tab1.SwitchOn : Tab1.SwitchOff;
        Tab2.SwitchSource = Tab2 == activeTab ? Tab2.SwitchOn : Tab2.SwitchOff;
        Tab3.SwitchSource = Tab3 == activeTab ? Tab3.SwitchOn : Tab3.SwitchOff;
        Tab4.SwitchSource = Tab4 == activeTab ? Tab4.SwitchOn : Tab4.SwitchOff;
        Tab5.SwitchSource = Tab5 == activeTab ? Tab5.SwitchOn : Tab5.SwitchOff;
        // Add logic here to change the content based on the active tab
    }
    public TransactionPage()
	{
		InitializeComponent(); 
        Tab1.SwitchSource = Tab1.SwitchOn;
        Tab2.SwitchSource = Tab2.SwitchOff;
        Tab3.SwitchSource = Tab3.SwitchOff;
        Tab4.SwitchSource = Tab4.SwitchOff;
        Tab5.SwitchSource = Tab5.SwitchOff;
    }

}