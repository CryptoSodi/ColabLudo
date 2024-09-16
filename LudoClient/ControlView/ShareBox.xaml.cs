namespace LudoClient.ControlView;

public partial class ShareBox : ContentView
{
    public void SetShareCode(string code)
    {
        shareCode.Text = code;
    }
    public ShareBox()
    {
        InitializeComponent();
    }
}