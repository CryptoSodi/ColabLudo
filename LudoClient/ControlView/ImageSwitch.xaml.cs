namespace LudoClient.ControlView;

public partial class ImageSwitch : ContentView
{
    public BindableProperty SwitchSourceProperty = BindableProperty.Create(nameof(SwitchSource), typeof(string), typeof(ImageSwitch), propertyChanged: (bindable, oldValue, newValue) =>
    {
        var control = (ImageSwitch)bindable;
        control.SwitchImage.Source = (string)newValue;
    });
    public string SwitchSource
    {
        get => GetValue(SwitchSourceProperty) as string;
        set => SetValue(SwitchSourceProperty, value);
    }
    public string SwitchOn = "switch_btn_on.png";
    public string SwitchOff = "switch_btn_off.png";
    public bool SwitchState = true;
    public ImageSwitch()
	{
		InitializeComponent();
        SwitchSource = SwitchOn;
    }

    private void SwitchImage_Clicked(object sender, EventArgs e)
    {
        if (SwitchState)
        {
            SwitchSource = SwitchOff;
        }
        else
        {
            SwitchSource = SwitchOn;
        }
        SwitchState = !SwitchState;
    }
}