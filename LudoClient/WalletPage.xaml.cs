using SharedCode.Constants;
namespace LudoClient;
public partial class WalletPage : ContentPage
{
    public WalletPage()
    {
        InitializeComponent();
        Coins.Text = UserInfo.Instance.Coins + "";
    }
}