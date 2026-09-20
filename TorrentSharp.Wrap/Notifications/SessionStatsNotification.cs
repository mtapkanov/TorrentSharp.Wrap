using TorrentSharp.Wrap.Imports.Events;

namespace TorrentSharp.Wrap.Notifications;

public class SessionStatsNotification(SessionStatsEvent @event, IReadOnlyDictionary<string, long> metrics) : SessionNotification(@event.Info)
{
    /// <summary>
    /// Every sampled metric, keyed by libtorrent's own metric name (e.g. <c>"net.recv_bytes"</c>).
    /// </summary>
    public IReadOnlyDictionary<string, long> Metrics { get; } = metrics;
}
