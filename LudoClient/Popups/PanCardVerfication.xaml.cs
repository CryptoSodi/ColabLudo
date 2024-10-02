namespace LudoClient.Popups;

public partial class PanCardVerfication : BasePopup
{
    public PanCardVerfication()
    {
        InitializeComponent();
        basePopup.capsule.ImageSource = "verification_status_main_bg.png";
    }
}