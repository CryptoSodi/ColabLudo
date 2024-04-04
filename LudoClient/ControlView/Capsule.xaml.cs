
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
}