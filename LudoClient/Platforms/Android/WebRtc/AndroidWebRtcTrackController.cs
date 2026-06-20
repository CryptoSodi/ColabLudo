using System.Text;
using System.Text.Json;
using Android.Content.PM;
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

    private readonly Dictionary<string, PeerConnection> _receivePeers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PeerConnection> _sendPeers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AudioTrack> _remoteAudioTracks = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, VideoTrack> _remoteVideoTracks = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _appliedAnswers = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _processedOffers = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _occupiedOpponents = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> _remoteAudioEnabled = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> _remoteVideoVisible = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _pendingReceiverResets = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _stateLock = new();
    private string? _lastPostedOffersJson;

    private SurfaceViewRenderer? _localPreviewRenderer;
    private readonly Dictionary<string, SurfaceViewRenderer> _seatRenderers = new(StringComparer.OrdinalIgnoreCase);

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

    public void SetLocalAudioEnabled(bool isEnabled)
    {
        try
        {
            _localAudioTrack?.SetEnabled(isEnabled);
            Console.WriteLine($"[NativeRTC] Local audio {(isEnabled ? "enabled" : "muted")}.");
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"[NativeRTC] Failed to change local audio state: {ex.Message}");
        }
    }

    public void SetLocalVideoEnabled(bool isEnabled)
    {
        try
        {
            _localVideoTrack?.SetEnabled(isEnabled);
            Console.WriteLine($"[NativeRTC] Local video {(isEnabled ? "enabled" : "disabled")}.");
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"[NativeRTC] Failed to change local video state: {ex.Message}");
        }
    }

    public void SetRemoteAudioEnabled(string playerColor, bool isEnabled)
    {
        if (string.IsNullOrWhiteSpace(playerColor))
            return;

        var key = playerColor.Trim().ToLowerInvariant();
        _remoteAudioEnabled[key] = isEnabled;

        try
        {
            if (_remoteAudioTracks.TryGetValue(key, out var track))
                track.SetEnabled(isEnabled);
            Console.WriteLine($"[NativeRTC] Remote audio {(isEnabled ? "enabled" : "muted")} for {key}.");
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"[NativeRTC] Failed to change remote audio state for {key}: {ex.Message}");
        }
    }

    public void SetRemoteVideoVisible(string playerColor, bool isVisible)
    {
        if (string.IsNullOrWhiteSpace(playerColor))
            return;

        var key = playerColor.Trim().ToLowerInvariant();
        _remoteVideoVisible[key] = isVisible;

        if (!_seatRenderers.TryGetValue(key, out var renderer))
            return;

        try
        {
            renderer.Post(() => renderer.Visibility = isVisible ? global::Android.Views.ViewStates.Visible : global::Android.Views.ViewStates.Invisible);
            Console.WriteLine($"[NativeRTC] Remote video {(isVisible ? "shown" : "hidden")} for {key}.");
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"[NativeRTC] Failed to change remote video visibility for {key}: {ex.Message}");
        }
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

        RtcHostRegistry.HostsChanged -= AttachRenderersToHosts;
        RtcHostRegistry.HostsChanged += AttachRenderersToHosts;
        EnsureSeatRenderers();
        EnsureLocalTracksReady();

        IsInitialized = true;
        Console.WriteLine("[NativeRTC] Android WebRTC initialized.");
    }

    public void StartSession(string roomId, string selfId, string apiBase, IReadOnlyCollection<string>? occupiedOpponentColors = null)
    {
        if (!IsInitialized)
            throw new InvalidOperationException("WebRTC track controller not initialized.");

        _roomId = roomId;
        _selfId = selfId;
        _apiBase = apiBase.TrimEnd('/');
        _isRunning = true;
        _hasRemoteVideo = false;
        _lastPostedOffersJson = null;
        _occupiedOpponents.Clear();
        if (occupiedOpponentColors != null)
        {
            foreach (var color in occupiedOpponentColors.Where(x => !string.IsNullOrWhiteSpace(x)))
                _occupiedOpponents.Add(color.Trim().ToLowerInvariant());
        }
        EnsureLocalTracksReady();
        AttachLocalTrackToSeat();
        AttachRenderersToHosts();
        Console.WriteLine($"[NativeRTC] Session started room={_roomId} self={_selfId} api={_apiBase}");
    }

    public async Task ExchangeTickAsync(CancellationToken cancellationToken)
    {
        if (!_isRunning || string.IsNullOrWhiteSpace(_roomId) || string.IsNullOrWhiteSpace(_selfId))
            return;

        ProcessPendingReceiverResets();

        using var http = new HttpClient();

        await RunBroadcasterTickAsync(http, cancellationToken);
        await RunReceiverTickAsync(http, cancellationToken);
    }

    public void StopLocalTracks()
    {
        _isRunning = false;

        lock (_stateLock)
        {
            foreach (var pc in _sendPeers.Values)
                pc.Dispose();
            _sendPeers.Clear();

            foreach (var pc in _receivePeers.Values)
                pc.Dispose();
            _receivePeers.Clear();
            _remoteAudioTracks.Clear();
            _remoteVideoTracks.Clear();
            _hasRemoteVideo = false;
            _lastPostedOffersJson = null;
            _occupiedOpponents.Clear();
            _remoteAudioEnabled.Clear();
            _remoteVideoVisible.Clear();
            _pendingReceiverResets.Clear();
        }

        DisposeLocalMedia();

        try { _localPreviewRenderer?.Release(); } catch { }
        _localPreviewRenderer = null;

        foreach (var renderer in _seatRenderers.Values)
            renderer.Release();
        _seatRenderers.Clear();
        RtcHostRegistry.HostsChanged -= AttachRenderersToHosts;
    }

    private void EnsureLocalTracksReady()
    {
        if (_localAudioTrack != null && _localVideoTrack != null && _capturer != null)
            return;

        if (!HasCapturePermissions())
        {
            Console.WriteLine("[NativeRTC] Camera/mic permissions not granted yet; delaying local track init.");
            return;
        }

        DisposeLocalMedia();
        InitializeLocalTracks();
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
        EnsureLocalPreviewRenderer();
        if (_localPreviewRenderer != null)
        {
            try { _localVideoTrack.AddSink(_localPreviewRenderer); } catch { }
        }
        AttachLocalTrackToSeat();

        HasLocalAudioTrack = true;
        HasLocalVideoTrack = true;
        Console.WriteLine("[NativeRTC] Local audio/video tracks initialized.");
    }

    private void DisposeLocalMedia()
    {
        try { _capturer?.StopCapture(); } catch { }
        _capturer?.Dispose();
        _capturer = null;

        _surfaceTextureHelper?.Dispose();
        _surfaceTextureHelper = null;

        _localVideoTrack?.Dispose();
        _localVideoTrack = null;

        _localAudioTrack?.Dispose();
        _localAudioTrack = null;

        _videoSource?.Dispose();
        _videoSource = null;

        _audioSource?.Dispose();
        _audioSource = null;

        HasLocalAudioTrack = false;
        HasLocalVideoTrack = false;
    }

    private void EnsureLocalPreviewRenderer()
    {
        if (_localPreviewRenderer != null || _eglBase == null)
            return;

        var activity = Platform.CurrentActivity;
        if (activity == null)
            return;

        activity.RunOnUiThread(() =>
        {
            if (_localPreviewRenderer != null || _eglBase == null)
                return;

            try
            {
                var renderer = new SurfaceViewRenderer(activity);
                renderer.Init(_eglBase.EglBaseContext, null);
                renderer.SetMirror(true);
                renderer.SetZOrderMediaOverlay(false);
                renderer.SetEnableHardwareScaler(true);
                _localPreviewRenderer = renderer;
                Console.WriteLine("[NativeRTC] Dedicated local preview renderer initialized.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NativeRTC] Failed to init dedicated local preview renderer: {ex.Message}");
            }
        });
    }

    private static bool HasCapturePermissions()
    {
        var activity = Platform.CurrentActivity;
        if (activity == null)
            return false;

        return activity.CheckSelfPermission(global::Android.Manifest.Permission.Camera) == Permission.Granted
            && activity.CheckSelfPermission(global::Android.Manifest.Permission.RecordAudio) == Permission.Granted;
    }

    private async Task RunBroadcasterTickAsync(HttpClient http, CancellationToken cancellationToken)
    {
        var targets = GetBroadcastTargets()
            .ToList();

        if (targets.Count == 0)
            return;

        List<string> pendingTargets;
        lock (_stateLock)
        {
            pendingTargets = targets.Where(t => !_sendPeers.ContainsKey(t)).ToList();
        }
        if (pendingTargets.Count > 0)
        {
            var offers = new List<object>();
            foreach (var target in pendingTargets)
            {
                var pc = CreateSenderPeer(target);
                lock (_stateLock)
                {
                    _sendPeers[target] = pc;
                }

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
                if (!string.Equals(_lastPostedOffersJson, offersJson, StringComparison.Ordinal))
                {
                    var content = JsonSerializer.Serialize(new { roomId = _roomId, playerColor = _selfId, offersJson });
                    var response = await http.PostAsync($"{_apiBase}/offers", new StringContent(content, Encoding.UTF8, "application/json"), cancellationToken);
                    Console.WriteLine($"[NativeRTC] POST offers => {(int)response.StatusCode}");
                    _lastPostedOffersJson = offersJson;
                }
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
            PeerConnection? pc;
            lock (_stateLock)
            {
                if (_appliedAnswers.Contains(key))
                    continue;
                if (!_sendPeers.TryGetValue(responder, out pc))
                    continue;
            }

            var answer = ParseSessionDescription(answerRaw);
            if (answer == null)
                continue;

            await SetRemoteDescriptionAsync(pc, answer, cancellationToken);
            lock (_stateLock)
            {
                _appliedAnswers.Add(key);
                _lastPostedOffersJson = null;
            }
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

            var updatedUtc = envelope.GetProperty("updatedUtc").ToString();

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
            var dedup = $"{_roomId}:{owner}:{_selfId}:{updatedUtc}";
            lock (_stateLock)
            {
                if (_processedOffers.Contains(dedup))
                    continue;
            }

            var offer = new SessionDescription(SessionDescription.SdpType.Offer, sdp);
            PeerConnection? receiverPeer;
            lock (_stateLock)
            {
                if (!_receivePeers.TryGetValue(owner, out receiverPeer))
                {
                    receiverPeer = CreateReceiverPeer(owner);
                    _receivePeers[owner] = receiverPeer;
                    Console.WriteLine($"[NativeRTC] Receiver peer created for owner={owner}");
                }
            }

            await SetRemoteDescriptionAsync(receiverPeer, offer, cancellationToken);
            var answer = await CreateAnswerAsync(receiverPeer, cancellationToken);
            if (answer == null)
                continue;
            Console.WriteLine($"[NativeRTC] Answer ready for owner={owner}. sdpLen={answer.Description?.Length ?? 0}");

            var answerJson = JsonSerializer.Serialize(new { type = answer.Type?.CanonicalForm() ?? "answer", sdp = answer.Description });
            var body = JsonSerializer.Serialize(new { roomId = _roomId, playerColor = _selfId, targetPlayerColor = owner, answerJson });
            var response = await http.PostAsync($"{_apiBase}/answers", new StringContent(body, Encoding.UTF8, "application/json"), cancellationToken);
            Console.WriteLine($"[NativeRTC] POST answers => {(int)response.StatusCode} owner={owner}");

            lock (_stateLock)
            {
                _processedOffers.Add(dedup);
            }
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

    private PeerConnection CreateReceiverPeer(string owner)
    {
        var pc = CreatePeerConnection(owner);
        Console.WriteLine($"[NativeRTC] Receiver peer created owner={owner}.");
        return pc;
    }

    private PeerConnection CreatePeerConnection(string? owner = null)
    {
        if (_factory == null)
            throw new InvalidOperationException("Factory is null.");

        var iceServers = new List<PeerConnection.IceServer>
        {
            PeerConnection.IceServer.InvokeBuilder(StunServer)!.CreateIceServer()
        };
        var rtcConfig = new PeerConnection.RTCConfiguration(iceServers);

        var observer = new PcObserver(this, owner);
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
        const int maxWaitMs = 450;
        const int stepMs = 50;
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

    private IEnumerable<string> GetBroadcastTargets()
    {
        if (_occupiedOpponents.Count > 0)
            return _occupiedOpponents;

        return GetSeatKeys().Where(x => !x.Equals(_selfId, StringComparison.OrdinalIgnoreCase));
    }

    private void EnsureSeatRenderers()
    {
        var activity = Platform.CurrentActivity;
        if (activity == null || _eglBase == null)
            return;

        activity.RunOnUiThread(() =>
        {
            try
            {
                foreach (var seat in GetSeatKeys())
                {
                    var host = RtcHostRegistry.GetHost(seat);
                    if (host == null)
                        continue;

                    if (_seatRenderers.TryGetValue(seat, out var existing) && !ReferenceEquals(existing, host))
                    {
                        try { existing.Release(); } catch { }
                        _seatRenderers.Remove(seat);
                    }

                    if (!_seatRenderers.TryGetValue(seat, out var renderer))
                    {
                        host.Init(_eglBase.EglBaseContext, null);
                        host.SetZOrderMediaOverlay(false);
                        host.SetEnableHardwareScaler(true);
                        _seatRenderers[seat] = host;
                        renderer = host;
                    }

                    renderer.SetMirror(seat.Equals(_selfId, StringComparison.OrdinalIgnoreCase));
                }

                foreach (var seat in GetSeatKeys().Where(seat => RtcHostRegistry.GetHost(seat) == null).ToList())
                {
                    if (_seatRenderers.TryGetValue(seat, out var staleRenderer))
                    {
                        try { staleRenderer.Release(); } catch { }
                        _seatRenderers.Remove(seat);
                    }
                }

                AttachLocalTrackToSeat();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"[NativeRTC] Failed to sync seat renderers: {ex.Message}");
            }
        });
    }

    private void AttachRenderersToHosts()
    {
        EnsureSeatRenderers();
    }

    private void AttachLocalTrackToSeat()
    {
        if (_localVideoTrack == null || string.IsNullOrWhiteSpace(_selfId))
            return;

        foreach (var entry in _seatRenderers)
        {
            try { _localVideoTrack.RemoveSink(entry.Value); } catch { }
            entry.Value.SetMirror(entry.Key.Equals(_selfId, StringComparison.OrdinalIgnoreCase));
        }

        if (_seatRenderers.TryGetValue(_selfId, out var renderer))
            _localVideoTrack.AddSink(renderer);
    }

    private void AttachRemoteTrack(string owner, VideoTrack? track)
    {
        if (track == null || !_seatRenderers.TryGetValue(owner, out var renderer))
            return;

        try
        {
            lock (_stateLock)
            {
                _remoteVideoTracks[owner] = track;
            }
            track.AddSink(renderer);
            ApplyRemoteVideoVisibility(owner, renderer);
            _hasRemoteVideo = true;
            Console.WriteLine($"[NativeRTC] Remote video track attached owner={owner}.");
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"[NativeRTC] Remote attach failed owner={owner}: {ex.Message}");
        }
    }

    private void HandleReceiverPeerStateChange(string owner, PeerConnection.IceConnectionState? newState)
    {
        if (string.IsNullOrWhiteSpace(owner) || newState == null)
            return;

        if (newState == PeerConnection.IceConnectionState.Failed ||
            newState == PeerConnection.IceConnectionState.Disconnected ||
            newState == PeerConnection.IceConnectionState.Closed)
        {
            RequestReceiverPeerReset(owner, $"ice={newState}");
        }
    }

    private void HandleReceiverPeerStateChange(string owner, PeerConnection.PeerConnectionState? newState)
    {
        if (string.IsNullOrWhiteSpace(owner) || newState == null)
            return;

        if (newState == PeerConnection.PeerConnectionState.Failed ||
            newState == PeerConnection.PeerConnectionState.Disconnected ||
            newState == PeerConnection.PeerConnectionState.Closed)
        {
            RequestReceiverPeerReset(owner, $"connection={newState}");
        }
    }

    private void RequestReceiverPeerReset(string owner, string reason)
    {
        lock (_stateLock)
        {
            _pendingReceiverResets.Add(owner);
        }
        Console.WriteLine($"[NativeRTC] Receiver peer reset queued owner={owner} {reason}.");
    }

    private void ProcessPendingReceiverResets()
    {
        List<string>? owners = null;
        lock (_stateLock)
        {
            if (_pendingReceiverResets.Count == 0)
                return;

            owners = _pendingReceiverResets.ToList();
            _pendingReceiverResets.Clear();
        }

        foreach (var owner in owners)
            ResetReceiverPeer(owner, allowOfferRetry: true);
    }

    private void ResetReceiverPeer(string owner, bool allowOfferRetry)
    {
        if (string.IsNullOrWhiteSpace(owner))
            return;

        PeerConnection? peer = null;
        lock (_stateLock)
        {
            if (_receivePeers.TryGetValue(owner, out peer))
                _receivePeers.Remove(owner);

            _remoteAudioTracks.Remove(owner);
            _remoteVideoTracks.Remove(owner);
            _hasRemoteVideo = _remoteVideoTracks.Count > 0;

            if (allowOfferRetry)
            {
                var prefix = $"{_roomId}:{owner}:{_selfId}:";
                _processedOffers.RemoveWhere(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            }
        }

        try { peer?.Dispose(); } catch { }
    }

    private void AttachRemoteAudioTrack(string owner, AudioTrack? track)
    {
        if (track == null || string.IsNullOrWhiteSpace(owner))
            return;

        try
        {
            bool isEnabled = true;
            lock (_stateLock)
            {
                _remoteAudioTracks[owner] = track;
                if (_remoteAudioEnabled.TryGetValue(owner, out var requested))
                    isEnabled = requested;
            }
            track.SetEnabled(isEnabled);
            Console.WriteLine($"[NativeRTC] Remote audio track attached owner={owner}.");
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"[NativeRTC] Remote audio attach failed owner={owner}: {ex.Message}");
        }
    }

    private void ApplyRemoteVideoVisibility(string owner, SurfaceViewRenderer renderer)
    {
        var isVisible = !_remoteVideoVisible.TryGetValue(owner, out var requestedVisible) || requestedVisible;
        renderer.Post(() => renderer.Visibility = isVisible ? global::Android.Views.ViewStates.Visible : global::Android.Views.ViewStates.Invisible);
    }

    private static string[] GetSeatKeys() => ["red", "green", "yellow", "blue"];

    private sealed class PcObserver(AndroidWebRtcTrackController owner, string? remoteOwner) : Java.Lang.Object, PeerConnection.IObserver
    {
        public void OnAddStream(MediaStream? stream)
        {
            if (stream == null || string.IsNullOrWhiteSpace(remoteOwner))
                return;

            if (stream.AudioTracks != null && stream.AudioTracks.Count > 0)
            {
                var audioTrack = stream.AudioTracks[0] as AudioTrack;
                owner.AttachRemoteAudioTrack(remoteOwner, audioTrack);
            }

            if (stream.VideoTracks != null && stream.VideoTracks.Count > 0)
            {
                var track = stream.VideoTracks[0] as VideoTrack;
                owner.AttachRemoteTrack(remoteOwner, track);
            }
        }

        public void OnAddTrack(RtpReceiver? receiver, MediaStream[]? mediaStreams) { }
        public void OnDataChannel(DataChannel? dataChannel) { }
        public void OnIceCandidate(IceCandidate? candidate) { }
        public void OnIceCandidateError(IceCandidateErrorEvent? e)
        {
            try
            {
                Console.WriteLine($"[NativeRTC] ICE candidate error url={e?.Address} port={e?.Port} code={e?.ErrorCode} text={e?.ErrorText}");
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"[NativeRTC] ICE candidate error logging failed: {ex.Message}");
            }
        }
        public void OnIceCandidatesRemoved(IceCandidate[]? candidates) { }
        public void OnIceConnectionChange(PeerConnection.IceConnectionState? newState)
        {
            Console.WriteLine($"[NativeRTC] ICE={newState}");
            if (!string.IsNullOrWhiteSpace(remoteOwner))
                owner.HandleReceiverPeerStateChange(remoteOwner, newState);
        }
        public void OnIceConnectionReceivingChange(bool p0) { }
        public void OnIceGatheringChange(PeerConnection.IceGatheringState? newState) { }
        public void OnRemoveStream(MediaStream? stream) { }
        public void OnRenegotiationNeeded() { }
        public void OnSignalingChange(PeerConnection.SignalingState? newState) => Console.WriteLine($"[NativeRTC] Signaling={newState}");
        public void OnStandardizedIceConnectionChange(PeerConnection.IceConnectionState? newState) { }
        public void OnConnectionChange(PeerConnection.PeerConnectionState? newState)
        {
            Console.WriteLine($"[NativeRTC] Connection={newState}");
            if (!string.IsNullOrWhiteSpace(remoteOwner))
                owner.HandleReceiverPeerStateChange(remoteOwner, newState);
        }
        public void OnTrack(RtpTransceiver? transceiver)
        {
            if (transceiver?.Receiver == null || string.IsNullOrWhiteSpace(remoteOwner))
                return;

            try
            {
                var track = transceiver.Receiver.Track();
                switch (track)
                {
                    case VideoTrack videoTrack:
                        owner.AttachRemoteTrack(remoteOwner, videoTrack);
                        break;
                    case AudioTrack audioTrack:
                        owner.AttachRemoteAudioTrack(remoteOwner, audioTrack);
                        break;
                }
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



