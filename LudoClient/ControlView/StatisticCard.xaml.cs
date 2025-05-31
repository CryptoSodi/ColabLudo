namespace LudoClient.ControlView;

public partial class StatisticCard : ContentView
{
    public StatisticCard()
    {
        InitializeComponent();
    }
    public BindableProperty TitleBarProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(StatisticCard), propertyChanged: (bindable, oldValue, newValue) =>
    {
        var control = (StatisticCard)bindable;
        control.TitleBarText.Text = (string)newValue;
    });
    public string Title
    {
        get => GetValue(TitleBarProperty) as string;
        set => SetValue(TitleBarProperty, value);
    }
    public void setValue(String value)
    {
        ValueText.Text = value;
    }
}