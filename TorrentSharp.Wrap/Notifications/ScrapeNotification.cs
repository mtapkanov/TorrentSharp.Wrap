using TorrentSharp.Wrap.Imports.Events;

namespace TorrentSharp.Wrap.Notifications;

public class ScrapeNotification(ScrapeEvent @event, TorrentManager torrentManager) : SessionNotification(@event.Info)
{
    public TorrentManager TorrentManager { get; } = torrentManager;

    /// <summary>
    /// Whether the scrape request completed successfully.
    /// </summary>
    public bool Succeeded { get; } = @event.Succeeded;

    /// <summary>
    /// Number of incomplete peers (leechers) reported by the tracker, or <c>-1</c> if unknown.
    /// </summary>
    public int Incomplete { get; } = @event.Incomplete;

    /// <summary>
    /// Number of complete peers (seeds) reported by the tracker, or <c>-1</c> if unknown.
    /// </summary>
    public int Complete { get; } = @event.Complete;
}
