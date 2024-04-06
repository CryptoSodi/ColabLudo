namespace LudoClient.ControlView;

public partial class DatePickerControl : ContentView
{
    public BindableProperty TitleLabelProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(DatePickerControl), propertyChanged: (bindable, oldValue, newValue) =>
    {
        var control = (DatePickerControl)bindable;
        control.TitleLabel.Text = (string)newValue;
    });
    public string Title
    {
        get => GetValue(TitleLabelProperty) as string;
        set => SetValue(TitleLabelProperty, value);
    }

    public DatePickerControl()
	{
		InitializeComponent();
	}
}