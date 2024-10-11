using CommunityToolkit.Maui.Views;
using LudoClient.ControlView;

namespace LudoClient.Popups;
public partial class BasePopup : Popup
{
    public BasePopup()
	{
		InitializeComponent();
        //// Get the device's main display information
        //var mainDisplayInfo = DeviceDisplay.MainDisplayInfo;
        //// Calculate the width and height in device-independent units
        //double width = mainDisplayInfo.Width / mainDisplayInfo.Density;
        //double height = mainDisplayInfo.Height / mainDisplayInfo.Density;
        //// Set the popup size
        //this.Size = new Size(width, height);
        CanBeDismissedByTappingOutsideOfPopup = false;
    }
    public static readonly BindableProperty PopupContentProperty = BindableProperty.Create(nameof(PopupContent), typeof(View), typeof(BasePopup), propertyChanged: OnPopupContentChanged);
    public View PopupContent
    {
        get => (View)GetValue(PopupContentProperty);
        set => SetValue(PopupContentProperty, value);
    }
    private static void OnPopupContentChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var popup = (BasePopup)bindable;
        if (newValue is View content )
        {
            popup.ContentContainer.Content = content;
        }
    }
    public static readonly BindableProperty ImageSourceProperty =
            BindableProperty.Create(
                nameof(ImageSource),
                typeof(ImageSource),
                typeof(BasePopup),
                default(ImageSource));

    public ImageSource ImageSource
    {
        get => (ImageSource)GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }
    private void OnBackgroundTapped(object sender, EventArgs e)
    {
        Close();
    }
}