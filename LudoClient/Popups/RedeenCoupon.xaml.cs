using CommunityToolkit.Maui.Views;
namespace LudoClient.Popups;

public partial class RedeenCoupon : BasePopup
{
    public RedeenCoupon()
    {
        InitializeComponent();
        basePopup.capsule.ImageSource = "signin_inner_bg.png";
    }
}