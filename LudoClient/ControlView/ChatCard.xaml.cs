using SharedCode;

namespace LudoClient.ControlView
{
    public partial class ChatCard : ContentView
    {
        public ChatMessages Message = new ChatMessages();
        public String cardActionType = "";
        public ChatCard()
        {
            InitializeComponent();
        }
        public void SetDetails(ChatMessages Message, String direction, String color)
        {
            PlayerImage.Source = Message.SenderPicture;
            MessageText.Text = Message.Message;
            TimeText.Text = Message.Time.ToShortTimeString();
            //detailgold.png
            switch (color)
            {
                case "red":
                    Layer.BackgroundColor = Color.FromArgb("#B11A1B"); // 20% transparent red
                    break;
                case "green":
                    Layer.BackgroundColor = Color.FromArgb("#017034"); // 20% transparent green
                    break;
                case "yellow":
                    Layer.BackgroundColor = Color.FromArgb("#BFA611");
                    // 20% transparent yellow
                    break;
                case "blue":
                    Layer.BackgroundColor = Color.FromArgb("#3166A6"); // 20% transparent blue
                    break;
                case "white":
                    Layer.BackgroundColor = Color.FromArgb("#E6FFFFFF"); // 20% transparent white
                    break;
            }

            if (direction == "Right")
            {
                Layer.ColumnDefinitions.Clear();
                Layer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                Layer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                // Swap columns
                Grid.SetColumn(PlayerGrid, 1);
                Grid.SetColumn(MessageText, 0);

                TimeText.HorizontalOptions = LayoutOptions.End;
            }
            else
            {

                TimeText.HorizontalOptions = LayoutOptions.Start;
            }
            //Message.
        }
    }
}