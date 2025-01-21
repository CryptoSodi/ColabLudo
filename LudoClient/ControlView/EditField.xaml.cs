namespace LudoClient.ControlView;

public partial class EditField : ContentView
{
    public Entry entryField;
    public BindableProperty TitleLabelProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(EditField), propertyChanged: (bindable, oldValue, newValue) =>
    {
        var control = (EditField)bindable;
        control.TitleLabel.Text = (string)newValue;
    });
    public string Title
    {
        get => GetValue(TitleLabelProperty) as string;
        set => SetValue(TitleLabelProperty, value);
    }
    public EditField()
    {
        InitializeComponent();
        this.entryField = EntryField;
    }
}