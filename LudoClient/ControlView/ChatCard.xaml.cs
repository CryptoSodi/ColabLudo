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
            this.Message = Message;
            MessageText.Text = Message.Message;
            TimeText.Text = Message.CreatedDate.ToLocalTime().ToShortTimeString();
            
            // Set Color based on seat or preference
            color = color?.ToLower() ?? "white";
            switch (color)
            {
                case "red":
                    BubbleBorder.BackgroundColor = Color.FromArgb("#B11A1B");
                    break;
                case "green":
                    BubbleBorder.BackgroundColor = Color.FromArgb("#017034");
                    break;
                case "yellow":
                    BubbleBorder.BackgroundColor = Color.FromArgb("#BFA611");
                    MessageText.TextColor = Colors.Black;
                    TimeText.TextColor = Colors.DarkSlateGray;
                    break;
                case "blue":
                    BubbleBorder.BackgroundColor = Color.FromArgb("#3166A6");
                    break;
                default: // white/gray
                    BubbleBorder.BackgroundColor = Color.FromArgb("#E6FFFFFF");
                    MessageText.TextColor = Colors.Black;
                    TimeText.TextColor = Colors.DarkSlateGray;
                    break;
            }

            if (direction == "Right")
            {
                BubbleBorder.HorizontalOptions = LayoutOptions.End;
                BubbleShape.CornerRadius = new CornerRadius(12, 12, 2, 12);
                
                RightAvatarBorder.IsVisible = true;
                RightAvatarImage.Source = Message.SenderPicture ?? "player.webp";
                LeftAvatarBorder.IsVisible = false;
            }
            else
            {
                BubbleBorder.HorizontalOptions = LayoutOptions.Start;
                BubbleShape.CornerRadius = new CornerRadius(12, 12, 12, 2);
                
                LeftAvatarBorder.IsVisible = true;
                LeftAvatarImage.Source = Message.SenderPicture ?? "player.webp";
                RightAvatarBorder.IsVisible = false;
                
                // Name label is now hidden as requested
                SenderNameLabel.IsVisible = false;
            }
        }
    }
}
