namespace LudoClient;
using Microsoft.Maui.Controls;
using System.Diagnostics;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();

    }
    protected override void OnSizeAllocated(double pageWidth, double pageHeight)
    {
        base.OnSizeAllocated(pageWidth, pageHeight);
        const double aspectRatio = 150 / 1236; // Aspect ratio of the original image
        //HeaderImage.WidthRequest = Math.Max(pageHeight * aspectRatio, pageWidth);
        //HeightRequest="{Binding Source={x:Reference HeaderImage}, Path=Height}"
        //Debug.WriteLine(HeaderImage.Height);
        //if (HeaderImage.Height > 0)
        {
             //PlayerImage.HeightRequest = HeaderImage.Height + 20;
        }
    }

    private void ImageButton_Clicked(object sender, EventArgs e)
    {
         Navigation.PushAsync(new GameRoom());
    }
}
