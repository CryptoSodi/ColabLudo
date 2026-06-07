namespace LudoClient.Services;

public interface IRtcClient
{
    Task StartAsync(string roomId, string playerColor, string apiBaseUrl, IReadOnlyCollection<string>? occupiedOpponentColors = null, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    void SetLocalAudioEnabled(bool isEnabled);
    void SetLocalVideoEnabled(bool isEnabled);
    void SetRemoteAudioEnabled(string playerColor, bool isEnabled);
    void SetRemoteVideoVisible(string playerColor, bool isVisible);
    bool IsRunning { get; }
}

public sealed class NullRtcClient : IRtcClient
{
    public bool IsRunning => false;

    public Task StartAsync(string roomId, string playerColor, string apiBaseUrl, IReadOnlyCollection<string>? occupiedOpponentColors = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void SetLocalAudioEnabled(bool isEnabled)
    {
    }

    public void SetLocalVideoEnabled(bool isEnabled)
    {
    }

    public void SetRemoteAudioEnabled(string playerColor, bool isEnabled)
    {
    }

    public void SetRemoteVideoVisible(string playerColor, bool isVisible)
    {
    }
}
