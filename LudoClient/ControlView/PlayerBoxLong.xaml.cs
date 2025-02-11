using CommunityToolkit.Maui.Views;
using LudoClient.Popups;
using SharedCode.Constants;

namespace LudoClient.ControlView;

public partial class PlayerBoxLong : ContentView
{
    public BindableProperty PlayerNameProperty = BindableProperty.Create(nameof(PlayerName), typeof(string), typeof(PlayerBoxLong), propertyChanged: (bindable, oldValue, newValue) =>
    {
        var control = (PlayerBoxLong)bindable;
        control.PlayerNameText.Text = (string)newValue;
    });
    public string PlayerName
    {
        get => GetValue(PlayerNameProperty) as string;
        set => SetValue(PlayerNameProperty, value);
    }
    public BindableProperty PlayerImageProperty = BindableProperty.Create(nameof(PlayerImage), typeof(string), typeof(PlayerBoxLong), propertyChanged: (bindable, oldValue, newValue) =>
    {
        var control = (PlayerBoxLong)bindable;
        control.PlayerImageItem.Source = (string)newValue;
    });
    public Image playerImageItem;

    public string PlayerImage
    {
        get => GetValue(PlayerImageProperty) as string;
        set => SetValue(PlayerImageProperty, value);
    }
    public PlayerBoxLong()
    {
        InitializeComponent();
        this.playerImageItem = PlayerImageItem;
    }
    private void EditInfoClicked(object sender, EventArgs e)
    {
        // Handle edit info button click
        Application.Current.MainPage.ShowPopup(new EditInfo());
        // Close the popup when the background is tapped
        //Close();
    }
}