using TorrentSharp.Wrap.Imports.Events;

namespace TorrentSharp.Wrap.Notifications;

/// <summary>
/// Raised when a torrent transitions into the error state (a disk error, a duplicate v2 torrent
/// resolving to the same hybrid hash, ...).
/// </summary>
public class TorrentErrorNotification(TorrentErrorEvent @event, TorrentManager torrentManager) : SessionNotification(@event.Info)
{
    public TorrentManager TorrentManager { get; } = torrentManager;

    /// <summary>The underlying OS/libtorrent error code.</summary>
    public int ErrorValue { get; } = @event.ErrorValue;

    /// <summary>The file (or object) the error occurred on, if applicable.</summary>
    public string Filename { get; } = @event.Filename;
}
