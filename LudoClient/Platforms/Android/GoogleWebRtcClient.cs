using LudoClient.Services;
using LudoClient.Platforms.Android.WebRtc;

namespace LudoClient.Platforms.Android;

internal sealed class GoogleWebRtcClient : IRtcClient
{
    private bool _isRunning;
    private readonly AndroidWebRtcTrackController _trackController = new();
    private CancellationTokenSource? _loopCts;
    private Task? _loopTask;

    public bool IsRunning => _isRunning;

    public Task StartAsync(string roomId, string playerColor, string apiBaseUrl, CancellationToken cancellationToken = default)
    {
        if (_isRunning)
            return Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(playerColor))
        {
            Console.WriteLine("[NativeRTC] Start skipped. Missing roomId/playerColor.");
            return Task.CompletedTask;
        }

        try
        {
            _trackController.Initialize();
            _trackController.StartSession(roomId, playerColor, apiBaseUrl);
            _isRunning = true;

            _loopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _loopTask = Task.Run(() => LoopAsync(_loopCts.Token), _loopCts.Token);

            Console.WriteLine($"[NativeRTC] Started. room={roomId} color={playerColor} api={apiBaseUrl}");
        }
        catch (System.Exception ex)
        {
            _isRunning = false;
            Console.WriteLine($"[NativeRTC] Start failed: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
            Console.WriteLine("[NativeRTC] Stop.");

        _loopCts?.Cancel();
        if (_loopTask != null)
        {
            try { await _loopTask.ConfigureAwait(false); } catch { }
        }

        _trackController.StopLocalTracks();
        _loopTask = null;
        _loopCts?.Dispose();
        _loopCts = null;
        _isRunning = false;
    }

    private async Task LoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await _trackController.ExchangeTickAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"[NativeRTC] Tick error: {ex.Message}");
            }

            try
            {
                var delayMs = _trackController.GetNextTickDelayMs();
                await Task.Delay(delayMs, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
