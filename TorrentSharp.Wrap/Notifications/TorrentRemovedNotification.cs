using TorrentSharp.Wrap.Imports.Events;

namespace TorrentSharp.Wrap.Notifications;

public class TorrentRemovedNotification(TorrentRemovedEvent @event, TorrentManager torrentManager) : SessionNotification(@event.Info)
{
    public TorrentManager TorrentManager { get; } = torrentManager;
}
