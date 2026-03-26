using Android.Views;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform;

namespace LudoClient.Platforms.Android
{
    // The MAUI-side Control
    public class NativeFriendCard : Microsoft.Maui.Controls.View
    {
        public SharedCode.PlayerCard Player { get; set; }
        public string CardType { get; set; }

        public NativeFriendCard(SharedCode.PlayerCard player, string cardType)
        {
            Player = player;
            CardType = cardType;
            HeightRequest = 65; // Set a default height that MAUI respects
            HorizontalOptions = Microsoft.Maui.Controls.LayoutOptions.Fill;
        }
    }

    // The Android-side Handler
    public class NativeFriendCardHandler : ViewHandler<NativeFriendCard, LudoClient.Platforms.Android.Popups.FriendDetailView>
    {
        public static PropertyMapper<NativeFriendCard, NativeFriendCardHandler> Mapper = new PropertyMapper<NativeFriendCard, NativeFriendCardHandler>(ViewHandler.ViewMapper)
        {
            [nameof(NativeFriendCard.Player)] = MapPlayer,
        };

        public NativeFriendCardHandler() : base(Mapper)
        {
        }

        protected override LudoClient.Platforms.Android.Popups.FriendDetailView CreatePlatformView()
        {
            // Following the "Best modern approach": Create/Inflate here
            var view = new LudoClient.Platforms.Android.Popups.FriendDetailView(Context);
            
            // Ensure it wants to fill the space MAUI gives it
            view.LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent);

            return view;
        }

        protected override void ConnectHandler(LudoClient.Platforms.Android.Popups.FriendDetailView platformView)
        {
            base.ConnectHandler(platformView);
            UpdatePlatformView();
        }

        static void MapPlayer(NativeFriendCardHandler handler, NativeFriendCard view)
        {
            handler.UpdatePlatformView();
        }

        void UpdatePlatformView()
        {
            if (PlatformView != null && VirtualView != null && VirtualView.Player != null)
            {
                PlatformView.SetDetails(VirtualView.Player, VirtualView.CardType);
                PlatformView.RequestLayout();
            }
        }

        // Bridging the measurement
        public override Microsoft.Maui.Graphics.Size GetDesiredSize(double widthConstraint, double heightConstraint)
        {
            if (PlatformView == null) return Microsoft.Maui.Graphics.Size.Zero;

            // Tell Android to measure itself
            int widthSpec = global::Android.Views.View.MeasureSpec.MakeMeasureSpec((int)Context.ToPixels(widthConstraint), global::Android.Views.MeasureSpecMode.AtMost);
            int heightSpec = global::Android.Views.View.MeasureSpec.MakeMeasureSpec(0, global::Android.Views.MeasureSpecMode.Unspecified);
            
            PlatformView.Measure(widthSpec, heightSpec);

            // Convert back to MAUI units
            return new Microsoft.Maui.Graphics.Size(
                Context.FromPixels(PlatformView.MeasuredWidth),
                Context.FromPixels(PlatformView.MeasuredHeight) > 0 ? Context.FromPixels(PlatformView.MeasuredHeight) : 65
            );
        }
    }
}
