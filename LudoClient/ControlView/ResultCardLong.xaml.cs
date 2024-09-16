namespace LudoClient.ControlView;

public partial class ResultCardLong : ContentView
{
    public BindableProperty BackGroundImageProperty = BindableProperty.Create(nameof(BackGroundImage), typeof(string), typeof(ResultCardLong), propertyChanged: (bindable, oldValue, newValue) =>
    {
        var control = (ResultCardLong)bindable;
        control.BgImageItem.Source = (string)newValue;
    });
    public string BackGroundImage
    {
        get => GetValue(BackGroundImageProperty) as string;
        set => SetValue(BackGroundImageProperty, value);
    }

    public BindableProperty BorderImageProperty = BindableProperty.Create(nameof(BorderImage), typeof(string), typeof(ResultCardLong), propertyChanged: (bindable, oldValue, newValue) =>
    {
        var control = (ResultCardLong)bindable;
        control.BorderImageItem.Source = (string)newValue;
    });
    public string BorderImage
    {
        get => GetValue(BorderImageProperty) as string;
        set => SetValue(BorderImageProperty, value);
    }

    public BindableProperty StarTypeItemProperty = BindableProperty.Create(nameof(StarImage), typeof(string), typeof(ResultCardLong), propertyChanged: (bindable, oldValue, newValue) =>
    {
        var control = (ResultCardLong)bindable;
        control.StarTypeItem.Source = (string)newValue;
    });
    public string StarImage
    {
        get => GetValue(StarTypeItemProperty) as string;
        set => SetValue(StarTypeItemProperty, value);
    }

    public BindableProperty PlayerNameProperty = BindableProperty.Create(nameof(PlayerName), typeof(string), typeof(ResultCardLong), propertyChanged: (bindable, oldValue, newValue) =>
    {
        var control = (ResultCardLong)bindable;
        control.PlayerNameItem.Text = (string)newValue;
    });
    public string PlayerName
    {
        get => GetValue(PlayerNameProperty) as string;
        set => SetValue(PlayerNameProperty, value);
    }



    public ResultCardLong()
    {
        InitializeComponent();
    }
}