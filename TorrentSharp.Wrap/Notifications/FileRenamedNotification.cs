using TorrentSharp.Wrap.Imports.Events;

namespace TorrentSharp.Wrap.Notifications;

public class FileRenamedNotification(FileRenamedEvent @event, TorrentManager torrentManager) : SessionNotification(@event.Info)
{
    public TorrentManager TorrentManager { get; } = torrentManager;

    /// <summary>
    /// Index of the file that was (or was meant to be) renamed.
    /// </summary>
    public int FileIndex { get; } = @event.FileIndex;

    /// <summary>
    /// Whether the rename completed successfully.
    /// </summary>
    public bool Succeeded { get; } = @event.Succeeded;
}
