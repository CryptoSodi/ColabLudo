using System.Collections.Concurrent;

namespace Ludo.Api.Services;

public class PlayerPresenceTracker
{
    private readonly ConcurrentDictionary<int, DateTime> _lastPingUtc = new();
    private readonly ConcurrentDictionary<int, byte> _onlineTransitionApplied = new();

    public void RecordPing(int playerId)
    {
        _lastPingUtc[playerId] = DateTime.UtcNow;
    }

    public IReadOnlyList<int> GetInactivePlayerIds(TimeSpan timeout, DateTime nowUtc)
    {
        var inactive = new List<int>();
        foreach (var kv in _lastPingUtc)
        {
            if (nowUtc - kv.Value > timeout)
                inactive.Add(kv.Key);
        }

        return inactive;
    }

    public void RemovePlayer(int playerId)
    {
        _lastPingUtc.TryRemove(playerId, out _);
        _onlineTransitionApplied.TryRemove(playerId, out _);
    }

    public bool TryMarkOnlineTransition(int playerId)
    {
        return _onlineTransitionApplied.TryAdd(playerId, 0);
    }
}
