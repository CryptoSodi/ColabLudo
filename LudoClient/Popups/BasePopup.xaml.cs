using CommunityToolkit.Maui.Views;
using LudoClient.ControlView;

namespace LudoClient.Popups;

public partial class BasePopup : Popup
{
    public Capsule capsule;
    public BasePopup()
	{
		InitializeComponent();
        // Get the device's main display information
        var mainDisplayInfo = DeviceDisplay.MainDisplayInfo;
        // Calculate the width and height in device-independent units
        double width = mainDisplayInfo.Width / mainDisplayInfo.Density;
        double height = mainDisplayInfo.Height / mainDisplayInfo.Density;
        // Set the popup size
        this.Size = new Size(width, height);
        capsule = capsuleImage;
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
    private void OnBackgroundTapped(object sender, EventArgs e)
    {
        Close();
    }
}