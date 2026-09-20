using TorrentSharp.Wrap.Imports.Events;

namespace TorrentSharp.Wrap.Notifications;

public class StorageMovedNotification(StorageMovedEvent @event, TorrentManager torrentManager) : SessionNotification(@event.Info)
{
    public TorrentManager TorrentManager { get; } = torrentManager;

    /// <summary>
    /// Whether the storage move completed successfully.
    /// </summary>
    public bool Succeeded { get; } = @event.Succeeded;
}
