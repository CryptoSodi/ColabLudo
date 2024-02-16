namespace LudoClient.ControlView;

public partial class TitleBar : ContentView
{
    public BindableProperty TitleBarProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(TitleBar), propertyChanged: (bindable, oldValue, newValue) =>
    {
        var control = (TitleBar)bindable;
        control.TitleBarText.Text = (string)newValue;
    });
    public string Title
    {
        get => GetValue(TitleBarProperty) as string;
        set => SetValue(TitleBarProperty, value);
    }
    
    public TitleBar()
	{
		InitializeComponent();
	}
}