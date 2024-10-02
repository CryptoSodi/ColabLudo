using CommunityToolkit.Maui.Views;
namespace LudoClient.Popups;
public partial class DailyBonus : BasePopup
{
    public DailyBonus()
    {
        InitializeComponent();
        basePopup.capsule.ImageSource = "support_popup_bg.png"; 
    }
}