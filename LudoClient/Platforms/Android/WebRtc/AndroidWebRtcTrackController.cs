using System.Text;
using System.Text.Json;
using Android.Views;
using Android.Widget;
using Java.Util.Concurrent;
using Microsoft.Maui.ApplicationModel;
using WebRtc.Android;

namespace LudoClient.Platforms.Android.WebRtc;

internal sealed class AndroidWebRtcTrackController
{
    private const string StunServer = "stun:stun.l.google.com:19302";

    private IEglBase? _eglBase;
    private PeerConnectionFactory? _factory;
    private VideoSource? _videoSource;
    private AudioSource? _audioSource;
    private VideoTrack? _localVideoTrack;
    private AudioTrack? _localAudioTrack;
    private ICameraVideoCapturer? _capturer;
    private SurfaceTextureHelper? _surfaceTextureHelper;
    private VideoCapturerAndroidObserver? _capturerObserver;

    private PeerConnection? _receivePeer;
    private string _receiveOwner = string.Empty;
    private readonly Dictionary<string, PeerConnection> _sendPeers = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _appliedAnswers = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _processedOffers = new(StringComparer.OrdinalIgnoreCase);

    private SurfaceViewRenderer? _localRenderer;
    private SurfaceViewRenderer? _remoteRenderer;
    private LinearLayout? _overlayRoot;

    private string _roomId = string.Empty;
    private string _selfId = string.Empty;
    private string _apiBase = string.Empty;
    private bool _isRunning;
    private volatile bool _hasRemoteVideo;

    public bool IsInitialized { get; private set; }
    public bool HasLocalAudioTrack { get; private set; }
    public bool HasLocalVideoTrack { get; private set; }
    public bool HasRemoteVideo => _hasRemoteVideo;

    public int GetNextTickDelayMs()
    {
        // Fast polling during handshake; back off once remote media is flowing.
        return _hasRemoteVideo ? 2500 : 350;
    }

    public void Initialize()
    {
        if (IsInitialized)
            return;

        var initOptions = PeerConnectionFactory.InitializationOptions
            .InvokeBuilder(Platform.AppContext)
            ?.CreateInitializationOptions();
        if (initOptions == null)
            throw new InvalidOperationException("WebRTC init options unavailable.");

        PeerConnectionFactory.Initialize(initOptions);

        _eglBase = EglBase.Create();
        PeerConnectionFactory.Options? options = null;

        var encoderFactory = new DefaultVideoEncoderFactory(_eglBase.EglBaseContext, true, true);
        var decoderFactory = new DefaultVideoDecoderFactory(_eglBase.EglBaseContext);

        _factory = PeerConnectionFactory.InvokeBuilder()
            .SetOptions(options)
            .SetVideoEncoderFactory(encoderFactory)
            .SetVideoDecoderFactory(decoderFactory)
            .CreatePeerConnectionFactory();

        BuildAndroidOverlay();
        InitializeLocalTracks();

        IsInitialized = true;
        Console.WriteLine("[NativeRTC] Android WebRTC initialized.");
    }

    public void StartSession(string roomId, string selfId, string apiBase)
    {
        if (!IsInitialized)
            throw new InvalidOperationException("WebRTC track controller not initialized.");

        _roomId = roomId;
        _selfId = selfId;
        _apiBase = apiBase.TrimEnd('/');
        _isRunning = true;
        _hasRemoteVideo = false;
        Console.WriteLine($"[NativeRTC] Session started room={_roomId} self={_selfId} api={_apiBase}");
    }

    public async Task ExchangeTickAsync(CancellationToken cancellationToken)
    {
        if (!_isRunning || string.IsNullOrWhiteSpace(_roomId) || string.IsNullOrWhiteSpace(_selfId))
            return;

        using var http = new HttpClient();

        await RunBroadcasterTickAsync(http, cancellationToken);
        await RunReceiverTickAsync(http, cancellationToken);
    }

    public void StopLocalTracks()
    {
        _isRunning = false;

        foreach (var pc in _sendPeers.Values)
            pc.Dispose();
        _sendPeers.Clear();

        _receivePeer?.Dispose();
        _receivePeer = null;
        _receiveOwner = string.Empty;
        _hasRemoteVideo = false;

        try { _capturer?.StopCapture(); } catch { }
        _capturer?.Dispose();
        _capturer = null;

        _surfaceTextureHelper?.Dispose();
        _surfaceTextureHelper = null;

        _localVideoTrack?.Dispose();
        _localVideoTrack = null;

        _localRenderer?.Release();
        _remoteRenderer?.Release();

        RemoveAndroidOverlay();

        HasLocalAudioTrack = false;
        HasLocalVideoTrack = false;
    }

    private void InitializeLocalTracks()
    {
        if (_factory == null || _eglBase == null)
            throw new InvalidOperationException("Factory/EGL not initialized.");

        _audioSource = _factory.CreateAudioSource(new MediaConstraints());
        _localAudioTrack = _factory.CreateAudioTrack("audio0", _audioSource);

        _videoSource = _factory.CreateVideoSource(false);
        _capturer = CreateCapturer();
        _surfaceTextureHelper = SurfaceTextureHelper.Create("CaptureThread", _eglBase.EglBaseContext);
        _capturerObserver = new VideoCapturerAndroidObserver(_videoSource.CapturerObserver);
        _capturer.Initialize(_surfaceTextureHelper, Platform.AppContext, _capturerObserver);
        _capturer.StartCapture(640, 480, 20);

        _localVideoTrack = _factory.CreateVideoTrack("video0", _videoSource);
        if (_localRenderer != null)
            _localVideoTrack.AddSink(_localRenderer);

        HasLocalAudioTrack = true;
        HasLocalVideoTrack = true;
    }

    private async Task RunBroadcasterTickAsync(HttpClient http, CancellationToken cancellationToken)
    {
        var targets = new[] { "red", "green", "yellow", "blue" }
            .Where(x => !x.Equals(_selfId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (targets.Count == 0)
            return;

        var pendingTargets = targets.Where(t => !_sendPeers.ContainsKey(t)).ToList();
        if (pendingTargets.Count > 0)
        {
            var offers = new List<object>();
            foreach (var target in pendingTargets)
            {
                var pc = CreateSenderPeer(target);
                _sendPeers[target] = pc;

                var offer = await CreateOfferAsync(pc, cancellationToken);
                if (offer != null)
                {
                    Console.WriteLine($"[NativeRTC] Offer ready for {target}. sdpLen={offer.Description?.Length ?? 0}");
                    offers.Add(new { clientId = target, offer = new { type = offer.Type?.CanonicalForm() ?? "offer", sdp = offer.Description } });
                }
            }

            if (offers.Count > 0)
            {
                var offersJson = JsonSerializer.Serialize(offers);
                var content = JsonSerializer.Serialize(new { roomId = _roomId, playerColor = _selfId, offersJson });
                var response = await http.PostAsync($"{_apiBase}/offers", new StringContent(content, Encoding.UTF8, "application/json"), cancellationToken);
                Console.WriteLine($"[NativeRTC] POST offers => {(int)response.StatusCode}");
            }
        }

        var answersResponse = await http.GetAsync($"{_apiBase}/answers?roomId={Uri.EscapeDataString(_roomId)}&playerColor={Uri.EscapeDataString(_selfId)}", cancellationToken);
        if (!answersResponse.IsSuccessStatusCode)
            return;

        var answersJson = await answersResponse.Content.ReadAsStringAsync(cancellationToken);
        using var answersDoc = JsonDocument.Parse(answersJson);
        if (answersDoc.RootElement.ValueKind != JsonValueKind.Array)
            return;

        foreach (var item in answersDoc.RootElement.EnumerateArray())
        {
            var responder = item.GetProperty("playerColor").GetString() ?? string.Empty;
            var answerRaw = item.GetProperty("answerJson").GetString() ?? string.Empty;
            var updatedUtc = item.GetProperty("updatedUtc").ToString();
            var key = $"{_roomId}:{_selfId}:{responder}:{updatedUtc}";
            if (_appliedAnswers.Contains(key))
                continue;
            if (!_sendPeers.TryGetValue(responder, out var pc))
                continue;

            var answer = ParseSessionDescription(answerRaw);
            if (answer == null)
                continue;

            await SetRemoteDescriptionAsync(pc, answer, cancellationToken);
            _appliedAnswers.Add(key);
            Console.WriteLine($"[NativeRTC] Answer applied for {responder}");
        }
    }

    private async Task RunReceiverTickAsync(HttpClient http, CancellationToken cancellationToken)
    {
        var offersResponse = await http.GetAsync($"{_apiBase}/offers?roomId={Uri.EscapeDataString(_roomId)}", cancellationToken);
        if (!offersResponse.IsSuccessStatusCode)
            return;

        var offersPayload = await offersResponse.Content.ReadAsStringAsync(cancellationToken);
        using var offersDoc = JsonDocument.Parse(offersPayload);
        if (offersDoc.RootElement.ValueKind != JsonValueKind.Array)
            return;

        foreach (var envelope in offersDoc.RootElement.EnumerateArray())
        {
            var owner = (envelope.GetProperty("playerColor").GetString() ?? string.Empty).ToLowerInvariant();
            if (owner == _selfId || string.IsNullOrWhiteSpace(owner))
                continue;

            var offersJson = envelope.GetProperty("offersJson").GetString() ?? "[]";
            using var listDoc = JsonDocument.Parse(offersJson);
            if (listDoc.RootElement.ValueKind != JsonValueKind.Array)
                continue;

            JsonElement mine = default;
            var found = false;
            foreach (var candidate in listDoc.RootElement.EnumerateArray())
            {
                var client = (candidate.GetProperty("clientId").GetString() ?? string.Empty).ToLowerInvariant();
                if (client == _selfId)
                {
                    mine = candidate;
                    found = true;
                    break;
                }
            }
            if (!found)
                continue;

            var offerElement = mine.GetProperty("offer");
            var sdp = offerElement.GetProperty("sdp").GetString() ?? string.Empty;
            var dedup = $"{_roomId}:{owner}:{_selfId}:{sdp.GetHashCode()}";
            if (_processedOffers.Contains(dedup))
                continue;

            var offer = new SessionDescription(SessionDescription.SdpType.Offer, sdp);
            if (_receivePeer == null || !_receiveOwner.Equals(owner, StringComparison.OrdinalIgnoreCase))
            {
                try { _receivePeer?.Dispose(); } catch { }
                _receivePeer = CreateReceiverPeer();
                _receiveOwner = owner;
                Console.WriteLine($"[NativeRTC] Receiver peer reset for owner={owner}");
            }

            await SetRemoteDescriptionAsync(_receivePeer, offer, cancellationToken);
            var answer = await CreateAnswerAsync(_receivePeer, cancellationToken);
            if (answer == null)
                continue;
            Console.WriteLine($"[NativeRTC] Answer ready for owner={owner}. sdpLen={answer.Description?.Length ?? 0}");

            var answerJson = JsonSerializer.Serialize(new { type = answer.Type?.CanonicalForm() ?? "answer", sdp = answer.Description });
            var body = JsonSerializer.Serialize(new { roomId = _roomId, playerColor = _selfId, targetPlayerColor = owner, answerJson });
            var response = await http.PostAsync($"{_apiBase}/answers", new StringContent(body, Encoding.UTF8, "application/json"), cancellationToken);
            Console.WriteLine($"[NativeRTC] POST answers => {(int)response.StatusCode} owner={owner}");

            _processedOffers.Add(dedup);
        }
    }

    private PeerConnection CreateSenderPeer(string target)
    {
        var pc = CreatePeerConnection();
        if (_localAudioTrack != null)
            pc.AddTrack(_localAudioTrack, new List<string> { "stream0" });
        if (_localVideoTrack != null)
            pc.AddTrack(_localVideoTrack, new List<string> { "stream0" });
        Console.WriteLine($"[NativeRTC] Sender peer created target={target}");
        return pc;
    }

    private PeerConnection CreateReceiverPeer()
    {
        var pc = CreatePeerConnection();
        Console.WriteLine("[NativeRTC] Receiver peer created.");
        return pc;
    }

    private PeerConnection CreatePeerConnection()
    {
        if (_factory == null)
            throw new InvalidOperationException("Factory is null.");

        var iceServers = new List<PeerConnection.IceServer>
        {
            PeerConnection.IceServer.InvokeBuilder(StunServer)!.CreateIceServer()
        };
        var rtcConfig = new PeerConnection.RTCConfiguration(iceServers);

        var observer = new PcObserver(this);
        var pc = _factory.CreatePeerConnection(rtcConfig, observer);
        if (pc == null)
            throw new InvalidOperationException("CreatePeerConnection failed.");

        return pc;
    }

    private async Task<SessionDescription?> CreateOfferAsync(PeerConnection pc, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<SessionDescription?>();
        var observer = new SdpObserverImpl(
            onCreateSuccess: sdp => tcs.TrySetResult(sdp),
            onFailure: err => tcs.TrySetException(new InvalidOperationException(err)));

        pc.CreateOffer(observer, new MediaConstraints());
        var offer = await tcs.Task.WaitAsync(ct);
        if (offer == null)
            return null;

        await SetLocalDescriptionAsync(pc, offer, ct);
        await WaitForIceGatheringCompleteAsync(pc, ct);
        return pc.LocalDescription ?? offer;
    }

    private async Task<SessionDescription?> CreateAnswerAsync(PeerConnection pc, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<SessionDescription?>();
        var observer = new SdpObserverImpl(
            onCreateSuccess: sdp => tcs.TrySetResult(sdp),
            onFailure: err => tcs.TrySetException(new InvalidOperationException(err)));

        pc.CreateAnswer(observer, new MediaConstraints());
        var answer = await tcs.Task.WaitAsync(ct);
        if (answer == null)
            return null;

        await SetLocalDescriptionAsync(pc, answer, ct);
        await WaitForIceGatheringCompleteAsync(pc, ct);
        return pc.LocalDescription ?? answer;
    }

    private static async Task WaitForIceGatheringCompleteAsync(PeerConnection pc, CancellationToken ct)
    {
        // Non-trickle signaling: wait briefly so SDP includes gathered ICE candidates.
        const int maxWaitMs = 1200;
        const int stepMs = 100;
        var waited = 0;

        while (waited < maxWaitMs && !ct.IsCancellationRequested)
        {
            var state = pc.InvokeIceGatheringState();
            if (state == PeerConnection.IceGatheringState.Complete)
                break;

            await Task.Delay(stepMs, ct);
            waited += stepMs;
        }
    }

    private static Task SetLocalDescriptionAsync(PeerConnection pc, SessionDescription sdp, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource();
        var observer = new SdpObserverImpl(
            onSetSuccess: () => tcs.TrySetResult(),
            onFailure: err => tcs.TrySetException(new InvalidOperationException(err)));
        pc.SetLocalDescription(observer, sdp);
        return tcs.Task.WaitAsync(ct);
    }

    private static Task SetRemoteDescriptionAsync(PeerConnection pc, SessionDescription sdp, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource();
        var observer = new SdpObserverImpl(
            onSetSuccess: () => tcs.TrySetResult(),
            onFailure: err => tcs.TrySetException(new InvalidOperationException(err)));
        pc.SetRemoteDescription(observer, sdp);
        return tcs.Task.WaitAsync(ct);
    }

    private static SessionDescription? ParseSessionDescription(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var type = doc.RootElement.GetProperty("type").GetString() ?? "answer";
            var sdp = doc.RootElement.GetProperty("sdp").GetString() ?? string.Empty;
            var sdpType = type.Equals("offer", StringComparison.OrdinalIgnoreCase)
                ? SessionDescription.SdpType.Offer
                : SessionDescription.SdpType.Answer;
            return new SessionDescription(sdpType, sdp);
        }
        catch
        {
            return null;
        }
    }

    private ICameraVideoCapturer CreateCapturer()
    {
        var enumerator = new Camera2Enumerator(Platform.AppContext);
        var names = enumerator.GetDeviceNames();
        foreach (var name in names)
        {
            if (enumerator.IsFrontFacing(name))
            {
                var cap = enumerator.CreateCapturer(name, null);
                if (cap != null)
                    return cap;
            }
        }

        foreach (var name in names)
        {
            var cap = enumerator.CreateCapturer(name, null);
            if (cap != null)
                return cap;
        }

        throw new InvalidOperationException("No camera capturer available.");
    }

    private void BuildAndroidOverlay()
    {
        var activity = Platform.CurrentActivity;
        if (activity == null || _eglBase == null)
            return;

        activity.RunOnUiThread(() =>
        {
            try
            {
                var root = activity.Window?.DecorView as ViewGroup;
                if (root == null)
                    return;

                _overlayRoot = new LinearLayout(activity)
                {
                    Orientation = Orientation.Horizontal,
                };
                var rootParams = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 320)
                {
                    Gravity = GravityFlags.Top
                };
                _overlayRoot.LayoutParameters = rootParams;

                _localRenderer = new SurfaceViewRenderer(activity);
                _remoteRenderer = new SurfaceViewRenderer(activity);

                _localRenderer.Init(_eglBase.EglBaseContext, null);
                _remoteRenderer.Init(_eglBase.EglBaseContext, null);
                _localRenderer.SetMirror(true);

                var childParams = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent, 1f);
                _overlayRoot.AddView(_localRenderer, childParams);
                _overlayRoot.AddView(_remoteRenderer, childParams);

                root.AddView(_overlayRoot);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"[NativeRTC] Failed to create overlay: {ex.Message}");
            }
        });
    }

    private void RemoveAndroidOverlay()
    {
        var activity = Platform.CurrentActivity;
        if (activity == null)
            return;

        activity.RunOnUiThread(() =>
        {
            try
            {
                if (_overlayRoot?.Parent is ViewGroup parent)
                    parent.RemoveView(_overlayRoot);
                _overlayRoot = null;
            }
            catch { }
        });
    }

    private sealed class PcObserver(AndroidWebRtcTrackController owner) : Java.Lang.Object, PeerConnection.IObserver
    {
        public void OnAddStream(MediaStream? stream)
        {
            if (stream == null || owner._remoteRenderer == null)
                return;

            if (stream.VideoTracks != null && stream.VideoTracks.Count > 0)
            {
                var track = stream.VideoTracks[0] as VideoTrack;
                track?.AddSink(owner._remoteRenderer);
                owner._hasRemoteVideo = true;
                Console.WriteLine("[NativeRTC] Remote video track attached.");
            }
        }

        public void OnAddTrack(RtpReceiver? receiver, MediaStream[]? mediaStreams) { }
        public void OnDataChannel(DataChannel? dataChannel) { }
        public void OnIceCandidate(IceCandidate? candidate) { }
        public void OnIceCandidatesRemoved(IceCandidate[]? candidates) { }
        public void OnIceConnectionChange(PeerConnection.IceConnectionState? newState) => Console.WriteLine($"[NativeRTC] ICE={newState}");
        public void OnIceConnectionReceivingChange(bool p0) { }
        public void OnIceGatheringChange(PeerConnection.IceGatheringState? newState) { }
        public void OnRemoveStream(MediaStream? stream) { }
        public void OnRenegotiationNeeded() { }
        public void OnSignalingChange(PeerConnection.SignalingState? newState) => Console.WriteLine($"[NativeRTC] Signaling={newState}");
        public void OnStandardizedIceConnectionChange(PeerConnection.IceConnectionState? newState) { }
        public void OnConnectionChange(PeerConnection.PeerConnectionState? newState) => Console.WriteLine($"[NativeRTC] Connection={newState}");
        public void OnTrack(RtpTransceiver? transceiver)
        {
            if (transceiver?.Receiver == null || owner._remoteRenderer == null)
                return;

            try
            {
                var track = transceiver.Receiver.Track() as VideoTrack;
                if (track == null)
                    return;

                track.AddSink(owner._remoteRenderer);
                owner._hasRemoteVideo = true;
                Console.WriteLine("[NativeRTC] Remote video track attached via OnTrack.");
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"[NativeRTC] OnTrack attach failed: {ex.Message}");
            }
        }
        public void OnSelectedCandidatePairChanged(CandidatePairChangeEvent? p0) { }
    }

    private sealed class SdpObserverImpl(
        Action<SessionDescription?>? onCreateSuccess = null,
        Action? onSetSuccess = null,
        Action<string>? onFailure = null) : Java.Lang.Object, ISdpObserver
    {
        public void OnCreateSuccess(SessionDescription? sdp) => onCreateSuccess?.Invoke(sdp);
        public void OnSetSuccess() => onSetSuccess?.Invoke();
        public void OnCreateFailure(string? error) => onFailure?.Invoke(error ?? "create failure");
        public void OnSetFailure(string? error) => onFailure?.Invoke(error ?? "set failure");
    }

    private sealed class VideoCapturerAndroidObserver(ICapturerObserver inner) : Java.Lang.Object, ICapturerObserver
    {
        public void OnCapturerStarted(bool success) => inner.OnCapturerStarted(success);
        public void OnCapturerStopped() => inner.OnCapturerStopped();
        public void OnFrameCaptured(VideoFrame? frame)
        {
            if (frame != null)
                inner.OnFrameCaptured(frame);
        }
    }
}



