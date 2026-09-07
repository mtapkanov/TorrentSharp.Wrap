using TorrentSharp.Wrap.Imports.Events;

namespace TorrentSharp.Wrap.Notifications;

public class MetadataReceivedNotification(MetadataReceivedEvent @event, TorrentManager torrentManager) : SessionNotification(@event.Info)
{
    public TorrentManager TorrentManager { get; } = torrentManager;
}
