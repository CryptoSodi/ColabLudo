using LudoClient.Controls;
using LudoClient.Services;

namespace LudoClient.CoreEngine;

internal sealed class GameRtcHelper
{
#if ANDROID
    private static readonly bool UseNativeGoogleWebRtc = true;
#else
    private static readonly bool UseNativeGoogleWebRtc = false;
#endif

    private const string WebRtcApiBaseUrl = "https://api.ludocities.com/api/webrtc";
    private const bool WebRtcDebug = false;

    private readonly AbsoluteLayout _boardHost;
    private readonly AbsoluteLayout _overlayHost;
    private readonly IRtcClient _rtcClient;

    private CancellationTokenSource? _rtcStartupCts;
    private bool _platformRtcViewsInitialized;
    private string _roomCode = string.Empty;
    private string _playerColor = string.Empty;
    private double _boardWidth;
    private double _boardHeight;
    private int _boardRotation;
    private bool _redVisible;
    private bool _greenVisible;
    private bool _yellowVisible;
    private bool _blueVisible;

    private CameraWebView? _cameraViewRed;
    private CameraWebView? _cameraViewGreen;
    private CameraWebView? _cameraViewYellow;
    private CameraWebView? _cameraViewBlue;
    private RtcHostView? _rtcHostRed;
    private RtcHostView? _rtcHostGreen;
    private RtcHostView? _rtcHostYellow;
    private RtcHostView? _rtcHostBlue;

    private bool _cameraViewRedReady;
    private bool _cameraViewGreenReady;
    private bool _cameraViewYellowReady;
    private bool _cameraViewBlueReady;

    public GameRtcHelper(AbsoluteLayout boardHost, AbsoluteLayout overlayHost)
    {
        _boardHost = boardHost;
        _overlayHost = overlayHost;

        try
        {
            var sp = Application.Current?.Handler?.MauiContext?.Services;
            _rtcClient = sp?.GetService(typeof(IRtcClient)) as IRtcClient ?? new NullRtcClient();
        }
        catch
        {
            _rtcClient = new NullRtcClient();
        }
    }

    public void OnAppearing(string roomCode, string playerColor)
    {
        _roomCode = roomCode ?? string.Empty;
        _playerColor = playerColor ?? string.Empty;
        _cameraViewRedReady = false;
        _cameraViewGreenReady = false;
        _cameraViewYellowReady = false;
        _cameraViewBlueReady = false;

        _rtcStartupCts?.Cancel();
        _rtcStartupCts = new CancellationTokenSource();
        var startupToken = _rtcStartupCts.Token;

        _overlayHost.Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(3), async () =>
        {
            if (startupToken.IsCancellationRequested)
                return;

            try
            {
                EnsurePlatformRtcViewsInitialized();
                ApplyVisualState();
                ConfigureRtcWebViewSources();
                await StartRtcAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebRTC] Deferred startup failed: {ex.Message}");
            }
        });
    }

    public Task OnDisappearingAsync()
    {
        _rtcStartupCts?.Cancel();
        return _rtcClient.StopAsync();
    }

    public void UpdateState(double boardWidth, double boardHeight, int boardRotation, bool redVisible, bool greenVisible, bool yellowVisible, bool blueVisible)
    {
        _boardWidth = boardWidth;
        _boardHeight = boardHeight;
        _boardRotation = boardRotation;
        _redVisible = redVisible;
        _greenVisible = greenVisible;
        _yellowVisible = yellowVisible;
        _blueVisible = blueVisible;
        ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        UpdateCameraViewVisibility();
        UpdateCameraWebViewLayouts();
        UpdateCameraViewRotation();
    }

    private void CameraViewRed_Navigated(object? sender, WebNavigatedEventArgs e)
    {
        _cameraViewRedReady = e.Result == WebNavigationResult.Success;
        Console.WriteLine($"[WebRTC] Red camera view navigated. Success={_cameraViewRedReady}, Url={e.Url}");
    }

    private void CameraViewGreen_Navigated(object? sender, WebNavigatedEventArgs e)
    {
        _cameraViewGreenReady = e.Result == WebNavigationResult.Success;
        Console.WriteLine($"[WebRTC] Green camera view navigated. Success={_cameraViewGreenReady}, Url={e.Url}");
    }

    private void CameraViewYellow_Navigated(object? sender, WebNavigatedEventArgs e)
    {
        _cameraViewYellowReady = e.Result == WebNavigationResult.Success;
        Console.WriteLine($"[WebRTC] Yellow camera view navigated. Success={_cameraViewYellowReady}, Url={e.Url}");
    }

    private void CameraViewBlue_Navigated(object? sender, WebNavigatedEventArgs e)
    {
        _cameraViewBlueReady = e.Result == WebNavigationResult.Success;
        Console.WriteLine($"[WebRTC] Blue camera view navigated. Success={_cameraViewBlueReady}, Url={e.Url}");
    }

    private void ConfigureRtcWebViewSources()
    {
        try
        {
            if (UseNativeGoogleWebRtc)
            {
                if (_cameraViewRed != null) _cameraViewRed.Source = null;
                if (_cameraViewGreen != null) _cameraViewGreen.Source = null;
                if (_cameraViewYellow != null) _cameraViewYellow.Source = null;
                if (_cameraViewBlue != null) _cameraViewBlue.Source = null;
                Console.WriteLine("[WebRTC] Native RTC mode active. WebViews disabled on this platform.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_roomCode))
                return;

            var selfId = (_playerColor ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(selfId))
                return;

            var room = Uri.EscapeDataString(_roomCode);
            var self = Uri.EscapeDataString(selfId);
            var apiBase = Uri.EscapeDataString(WebRtcApiBaseUrl);
            var debug = WebRtcDebug ? "1" : "0";
            var broadcasterUrl = $"https://www.ludocities.com/broadcaster.html#roomId={room}&playerColor={self}&apiBase={apiBase}&debug={debug}";
            var receiverUrl = $"https://www.ludocities.com/receiver.html#roomId={room}&playerColor={self}&apiBase={apiBase}&debug={debug}";

            Console.WriteLine($"[WebRTC] Configuring RTC for room={_roomCode}, self={selfId}");
            if (_cameraViewRed != null) _cameraViewRed.Source = broadcasterUrl;
            if (_cameraViewGreen != null) _cameraViewGreen.Source = receiverUrl;
            if (_cameraViewYellow != null) _cameraViewYellow.Source = receiverUrl;
            if (_cameraViewBlue != null) _cameraViewBlue.Source = receiverUrl;
            ApplyVisualState();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebRTC] Failed to configure webview sources: {ex.Message}");
        }
    }

    private void UpdateCameraViewVisibility()
    {
        if (UseNativeGoogleWebRtc)
        {
            SyncPlatformView(_cameraViewRed, _boardHost, false);
            SyncPlatformView(_cameraViewGreen, _boardHost, false);
            SyncPlatformView(_cameraViewYellow, _boardHost, false);
            SyncPlatformView(_cameraViewBlue, _boardHost, false);
            SyncPlatformView(_rtcHostRed, _overlayHost, _redVisible);
            SyncPlatformView(_rtcHostGreen, _overlayHost, _greenVisible);
            SyncPlatformView(_rtcHostYellow, _overlayHost, _yellowVisible);
            SyncPlatformView(_rtcHostBlue, _overlayHost, _blueVisible);
            return;
        }

        SyncPlatformView(_cameraViewRed, _boardHost, _redVisible);
        SyncPlatformView(_cameraViewGreen, _boardHost, _greenVisible);
        SyncPlatformView(_cameraViewYellow, _boardHost, _yellowVisible);
        SyncPlatformView(_cameraViewBlue, _boardHost, _blueVisible);
        SyncPlatformView(_rtcHostRed, _overlayHost, false);
        SyncPlatformView(_rtcHostGreen, _overlayHost, false);
        SyncPlatformView(_rtcHostYellow, _overlayHost, false);
        SyncPlatformView(_rtcHostBlue, _overlayHost, false);
    }

    private void UpdateCameraWebViewLayouts()
    {
        try
        {
            if (_boardWidth <= 0 || _boardHeight <= 0)
                return;

            _overlayHost.WidthRequest = _boardWidth;
            _overlayHost.HeightRequest = _boardHeight;

            var width = (_boardWidth / 15.0) * 6.0;
            var height = (_boardHeight / 15.0) * 6.0;
            SetCameraViewBounds(_cameraViewRed, 0, 0, width, height);
            SetCameraViewBounds(_cameraViewGreen, _boardWidth - width, 0, width, height);
            SetCameraViewBounds(_cameraViewYellow, _boardWidth - width, _boardHeight - height, width, height);
            SetCameraViewBounds(_cameraViewBlue, 0, _boardHeight - height, width, height);
            SetCameraViewBounds(_rtcHostRed, 0, 0, width, height);
            SetCameraViewBounds(_rtcHostGreen, _boardWidth - width, 0, width, height);
            SetCameraViewBounds(_rtcHostYellow, _boardWidth - width, _boardHeight - height, width, height);
            SetCameraViewBounds(_rtcHostBlue, 0, _boardHeight - height, width, height);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebRTC] Failed to update camera layouts: {ex.Message}");
        }
    }

    private static void SetCameraViewBounds(View? view, double x, double y, double width, double height)
    {
        if (view == null)
            return;

        view.WidthRequest = width;
        view.HeightRequest = height;
        AbsoluteLayout.SetLayoutBounds(view, new Rect(x, y, width, height));
        AbsoluteLayout.SetLayoutFlags(view, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
    }

    private async void UpdateCameraViewRotation()
    {
        try
        {
            var uprightRotation = -_boardRotation;
#if ANDROID
            if (UseNativeGoogleWebRtc)
            {
                await RotateViewsAsync(uprightRotation, _cameraViewRed, _cameraViewGreen, _cameraViewYellow, _cameraViewBlue);
                SetRotation(_rtcHostRed, 0);
                SetRotation(_rtcHostGreen, 0);
                SetRotation(_rtcHostYellow, 0);
                SetRotation(_rtcHostBlue, 0);
                return;
            }
#endif
            await RotateViewsAsync(uprightRotation, _cameraViewRed, _cameraViewGreen, _cameraViewYellow, _cameraViewBlue, _rtcHostRed, _rtcHostGreen, _rtcHostYellow, _rtcHostBlue);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebRTC] Failed to update camera rotation: {ex.Message}");
        }
    }

    private void EnsurePlatformRtcViewsInitialized()
    {
        if (_platformRtcViewsInitialized)
            return;

        if (UseNativeGoogleWebRtc)
        {
            _rtcHostRed = CreateRtcHostView("red");
            _rtcHostGreen = CreateRtcHostView("green");
            _rtcHostYellow = CreateRtcHostView("yellow");
            _rtcHostBlue = CreateRtcHostView("blue");
        }
        else
        {
            _cameraViewRed = CreateCameraWebView(Colors.Red, "https://www.ludocities.com/broadcaster.html", CameraViewRed_Navigated);
            _cameraViewGreen = CreateCameraWebView(Colors.Green, "https://www.ludocities.com/receiver.html", CameraViewGreen_Navigated);
            _cameraViewYellow = CreateCameraWebView(Colors.Yellow, "https://www.ludocities.com/receiver.html", CameraViewYellow_Navigated);
            _cameraViewBlue = CreateCameraWebView(Colors.Blue, "https://www.ludocities.com/receiver.html", CameraViewBlue_Navigated);
        }

        _platformRtcViewsInitialized = true;
    }

    private static CameraWebView CreateCameraWebView(Color backgroundColor, string source, EventHandler<WebNavigatedEventArgs> navigatedHandler)
    {
        var view = new CameraWebView
        {
            ZIndex = 1,
            BackgroundColor = backgroundColor,
            InputTransparent = true,
            Source = source
        };
        view.Navigated += navigatedHandler;
        return view;
    }

    private static RtcHostView CreateRtcHostView(string seatColor)
    {
        return new RtcHostView
        {
            SeatColor = seatColor,
            ZIndex = 1,
            BackgroundColor = Colors.Transparent,
            InputTransparent = true
        };
    }

    private static void SyncPlatformView(View? view, Layout parent, bool shouldAttach)
    {
        if (view == null)
            return;

        if (shouldAttach)
        {
            if (view.Parent is Layout currentParent && !ReferenceEquals(currentParent, parent))
                currentParent.Remove(view);

            if (!parent.Contains(view))
                parent.Add(view);

            return;
        }

        if (view.Parent is Layout attachedParent && attachedParent.Contains(view))
            attachedParent.Remove(view);
    }

    private static async Task RotateViewsAsync(double rotation, params View?[] views)
    {
        var tasks = views
            .Where(view => view != null)
            .Select(view => view!.RotateTo(rotation, 1, Easing.CubicIn))
            .ToArray();

        if (tasks.Length > 0)
            await Task.WhenAll(tasks);
    }

    private static void SetRotation(View? view, double rotation)
    {
        if (view != null)
            view.Rotation = rotation;
    }

    private async Task StartRtcAsync()
    {
        try
        {
            if (!UseNativeGoogleWebRtc)
                return;
            if (string.IsNullOrWhiteSpace(_roomCode))
                return;
            var selfId = (_playerColor ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(selfId))
                return;

            await _rtcClient.StartAsync(_roomCode, selfId, WebRtcApiBaseUrl, GetOccupiedOpponentColors());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NativeRTC] Start failed: {ex.Message}");
        }
    }

    private IReadOnlyCollection<string> GetOccupiedOpponentColors()
    {
        var self = (_playerColor ?? string.Empty).Trim().ToLowerInvariant();
        var occupied = new List<string>(4);

        AddIfOpponent(occupied, "red", _redVisible, self);
        AddIfOpponent(occupied, "green", _greenVisible, self);
        AddIfOpponent(occupied, "yellow", _yellowVisible, self);
        AddIfOpponent(occupied, "blue", _blueVisible, self);

        return occupied;
    }

    private static void AddIfOpponent(List<string> occupied, string color, bool isVisible, string self)
    {
        if (isVisible && !color.Equals(self, StringComparison.OrdinalIgnoreCase))
            occupied.Add(color);
    }
}
