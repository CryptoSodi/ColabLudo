using WebRtc.Android;

namespace LudoClient.Platforms.Android;

internal static class RtcHostRegistry
{
    private static readonly Dictionary<string, SurfaceViewRenderer> Hosts = new(StringComparer.OrdinalIgnoreCase);

    public static event Action? HostsChanged;

    public static void Register(string seatColor, SurfaceViewRenderer host)
    {
        if (string.IsNullOrWhiteSpace(seatColor))
            return;

        lock (Hosts)
        {
            Hosts[seatColor] = host;
        }

        HostsChanged?.Invoke();
    }

    public static void Unregister(string seatColor, SurfaceViewRenderer host)
    {
        if (string.IsNullOrWhiteSpace(seatColor))
            return;

        lock (Hosts)
        {
            if (Hosts.TryGetValue(seatColor, out var existing) && ReferenceEquals(existing, host))
                Hosts.Remove(seatColor);
        }

        HostsChanged?.Invoke();
    }

    public static SurfaceViewRenderer? GetHost(string seatColor)
    {
        lock (Hosts)
        {
            return Hosts.TryGetValue(seatColor, out var host) ? host : null;
        }
    }
}
