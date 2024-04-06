namespace LudoClient.ControlView;

public partial class DropDown : ContentView
{
    public BindableProperty TitleLabelProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(DropDown), propertyChanged: (bindable, oldValue, newValue) =>
    {
        var control = (DropDown)bindable;
        control.TitleLabel.Text = (string)newValue;
    });
    public string Title
    {
        get => GetValue(TitleLabelProperty) as string;
        set => SetValue(TitleLabelProperty, value);
    }
    public DropDown()
    {
        InitializeComponent();
    }
}