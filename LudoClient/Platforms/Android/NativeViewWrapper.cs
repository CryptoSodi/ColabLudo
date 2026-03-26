using Android.Views;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Platform;

namespace LudoClient.Platforms.Android
{
    public class NativeViewWrapper : Microsoft.Maui.Controls.View
    {
        public static readonly Microsoft.Maui.Controls.BindableProperty NativeViewProperty =
            Microsoft.Maui.Controls.BindableProperty.Create(nameof(NativeView), typeof(global::Android.Views.View), typeof(NativeViewWrapper), null);

        public global::Android.Views.View NativeView
        {
            get => (global::Android.Views.View)GetValue(NativeViewProperty);
            set => SetValue(NativeViewProperty, value);
        }

        public NativeViewWrapper(global::Android.Views.View nativeView)
        {
            NativeView = nativeView;
        }

        public void UpdateDetails(SharedCode.PlayerCard pc, string type)
        {
            if (NativeView is LudoClient.Platforms.Android.Popups.FriendDetailView fdv)
            {
                fdv.SetDetails(pc, type);
                fdv.RequestLayout();
                fdv.Invalidate();
            }
        }
    }

    public class NativeViewWrapperHandler : ViewHandler<NativeViewWrapper, global::Android.Views.View>
    {
        public static PropertyMapper<NativeViewWrapper, NativeViewWrapperHandler> Mapper = new PropertyMapper<NativeViewWrapper, NativeViewWrapperHandler>(ViewHandler.ViewMapper);

        public NativeViewWrapperHandler() : base(Mapper)
        {
        }

        protected override global::Android.Views.View CreatePlatformView()
        {
            return VirtualView.NativeView;
        }

        public override void PlatformArrange(Rect frame)
        {
            base.PlatformArrange(frame);

            if (PlatformView != null)
            {
                int width = (int)Context.ToPixels(frame.Width);
                int height = (int)Context.ToPixels(frame.Height);

                PlatformView.Measure(
                    global::Android.Views.View.MeasureSpec.MakeMeasureSpec(width, MeasureSpecMode.Exactly),
                    global::Android.Views.View.MeasureSpec.MakeMeasureSpec(height, MeasureSpecMode.Exactly));

                PlatformView.Layout(0, 0, width, height);
                PlatformView.Invalidate();
            }
        }

        public override Microsoft.Maui.Graphics.Size GetDesiredSize(double widthConstraint, double heightConstraint)
        {
            double width = VirtualView.WidthRequest > 0 ? VirtualView.WidthRequest : (double.IsInfinity(widthConstraint) ? 300 : widthConstraint);
            double height = VirtualView.HeightRequest > 0 ? VirtualView.HeightRequest : (double.IsInfinity(heightConstraint) ? 60 : heightConstraint);

            return new Microsoft.Maui.Graphics.Size(width, height);
        }
        
        protected override void ConnectHandler(global::Android.Views.View platformView)
        {
            base.ConnectHandler(platformView);
            
            if (platformView.Parent is ViewGroup parent)
            {
                parent.RemoveView(platformView);
            }

            platformView.LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent);

            platformView.RequestLayout();
        }
    }
}
