using LudoClient.Controls;
using Microsoft.Maui.Handlers;
using WebRtc.Android;

namespace LudoClient.Platforms.Android;

internal sealed class RtcHostViewHandler : ViewHandler<RtcHostView, SurfaceViewRenderer>
{
    public static readonly PropertyMapper<RtcHostView, RtcHostViewHandler> Mapper = new(ViewHandler.ViewMapper)
    {
        [nameof(RtcHostView.SeatColor)] = MapSeatColor
    };

    public RtcHostViewHandler() : base(Mapper)
    {
    }

    protected override SurfaceViewRenderer CreatePlatformView()
    {
        return new SurfaceViewRenderer(Context);
    }

    protected override void ConnectHandler(SurfaceViewRenderer platformView)
    {
        base.ConnectHandler(platformView);
        RegisterHost(VirtualView, platformView);
    }

    protected override void DisconnectHandler(SurfaceViewRenderer platformView)
    {
        UnregisterHost(VirtualView, platformView);
        base.DisconnectHandler(platformView);
    }

    public static void MapSeatColor(RtcHostViewHandler handler, RtcHostView view)
    {
        if (handler.PlatformView == null)
            return;

        RegisterHost(view, handler.PlatformView);
    }

    private static void RegisterHost(RtcHostView? view, SurfaceViewRenderer platformView)
    {
        if (view == null)
            return;

        RtcHostRegistry.Register(view.SeatColor, platformView);
    }

    private static void UnregisterHost(RtcHostView? view, SurfaceViewRenderer platformView)
    {
        if (view == null)
            return;

        RtcHostRegistry.Unregister(view.SeatColor, platformView);
    }
}
