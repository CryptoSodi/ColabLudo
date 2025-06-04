using CommunityToolkit.Maui.Views;
using LudoClient.Constants;
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
    int remainderScore = 10;
    public void SetScore(int score, bool verified)
    {
        remainderScore = score % 10000; // Store the remainder
        int dividedScore = score / 10000;
        ScoreText.Text = dividedScore.ToString();
        if (verified)
        {
            VerificationImage.Source = "lbl_verified.png";
        }
        else
        {
            VerificationImage.Source = "lbl_unverified.png";
        }
        UpdateOrangeBarWidth();
    }

    private void UpdateOrangeBarWidth()
    {
        double fullWidth = ScoreBarGrid.Width;

        if (fullWidth <= 0)
        {
            Task.Delay(200).ContinueWith(_ => UpdateOrangeBarWidth(), TaskScheduler.FromCurrentSynchronizationContext());
            // Wait until the layout is measured            
        }
        else
        {
            Task.Delay(100).ContinueWith(_ => SetOrangeBarWidth(), TaskScheduler.FromCurrentSynchronizationContext());
        }
    }

    private void SetOrangeBarWidth()
    {
        double fullWidth = ScoreBarGrid.Width;
        double targetWidth = fullWidth * remainderScore / 10000.0;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ReminderScoreText.Text = remainderScore.ToString();
            OrangeBar.WidthRequest = targetWidth;
        });
    }

    private void EditInfoClicked(object sender, EventArgs e)
    {

        Application.Current.MainPage.ShowPopup(ClientGlobalConstants.editInfo);
        // Handle edit info button click
        // Close the popup when the background is tapped
        //Close();
    }
}