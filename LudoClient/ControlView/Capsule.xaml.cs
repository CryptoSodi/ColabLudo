namespace LudoClient.ControlView;

public partial class Capsule : ContentView
{
    // Define the ImageSource bindable property
    public static readonly BindableProperty ImageSourceProperty = BindableProperty.Create(
        propertyName: nameof(ImageSource),
        returnType: typeof(ImageSource),
        declaringType: typeof(Capsule),
        defaultValue: default(ImageSource));

    // Property to get and set the ImageSource
    public ImageSource ImageSource
    {
        get => (ImageSource)GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }
    public Capsule()
    {
        InitializeComponent();
    }
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // 1. Find the elements inside the ControlTemplate
        var image = (Image)GetTemplateChild("ImageSourceContainer");
        var overlayGrid = (Grid)GetTemplateChild("OverlayGrid");

        if (image != null && overlayGrid != null)
        {
            // 2. Instantly update the Grid size whenever the Image size is calculated
            image.SizeChanged += (sender, args) =>
            {
                if (image.Width > 0 && image.Height > 0)
                {
                    overlayGrid.WidthRequest = image.Width;
                    overlayGrid.HeightRequest = image.Height;
                }
            };
        }
    }
}