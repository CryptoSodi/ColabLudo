namespace LudoClient.Controls;

internal class RtcHostView : ContentView
{
    public static readonly BindableProperty SeatColorProperty = BindableProperty.Create(
        nameof(SeatColor),
        typeof(string),
        typeof(RtcHostView),
        string.Empty);

    public string SeatColor
    {
        get => (string)GetValue(SeatColorProperty);
        set => SetValue(SeatColorProperty, value);
    }
}
