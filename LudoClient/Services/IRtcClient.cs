namespace LudoClient.Services;

public interface IRtcClient
{
    Task StartAsync(string roomId, string playerColor, string apiBaseUrl, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    bool IsRunning { get; }
}

public sealed class NullRtcClient : IRtcClient
{
    public bool IsRunning => false;

    public Task StartAsync(string roomId, string playerColor, string apiBaseUrl, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
