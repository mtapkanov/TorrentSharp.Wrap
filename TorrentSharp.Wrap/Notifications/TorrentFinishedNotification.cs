using TorrentSharp.Wrap.Imports.Events;

namespace TorrentSharp.Wrap.Notifications;

/// <summary>Raised when a torrent finishes downloading all its selected files.</summary>
public class TorrentFinishedNotification(TorrentFinishedEvent @event, TorrentManager torrentManager) : SessionNotification(@event.Info)
{
    public TorrentManager TorrentManager { get; } = torrentManager;
}
