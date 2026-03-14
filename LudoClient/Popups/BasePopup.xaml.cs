using CommunityToolkit.Maui.Views;

namespace LudoClient.Popups;
public partial class BasePopup : Popup
{
    public ContentView PopupContentContainer => ContentContainer;
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
        CanBeDismissedByTappingOutsideOfPopup = true; 
        // When handler is created, strip the border
        // Hook SizeChanged
        //ContentContainer.SizeChanged += OnCapsuleSized;
        //OnCapsuleSized(null, null);
    }
    public async void OnCapsuleSized(object? sender, EventArgs e)
    {
        Console.WriteLine("RESIZED");
        var parent = ContentContainer.Parent as Layout;
        if (parent != null)
        {   
            parent.Children.Remove(ContentContainer);
            await Task.Delay(50); // Let MAUI clear it out
            parent.Children.Add(ContentContainer); // Re-add

#if ANDROID
            try
            {
                // Access native Android view
                var platformView = ContentContainer.Handler?.PlatformView as Android.Views.View;
                if (platformView != null)
                {
                    // Temporarily stop drawing
                    platformView.SetWillNotDraw(true);

                    // Allow drawing again
                    platformView.SetWillNotDraw(false);

                    // Force invalidate & layout
                    platformView.Invalidate();
                    platformView.RequestLayout();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SkiaFlush] Error: {ex.Message}");
            }
#endif
        }



        //if (ContentContainer.Width > 0 && ContentContainer.Height > 0)
        //{
        //    // Proper delay
        //    

        //    // Capsule has been sized; make content visible
        //    ContentContainer.IsVisible = true;

        //    // Optional: Animate content fade-in


        //    // Remove handler to prevent repeated calls
        //    // CapsuleContainer.SizeChanged -= OnCapsuleSized;
        //}
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
        CloseAsync();
    }
}