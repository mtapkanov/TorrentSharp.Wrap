using TorrentSharp.Wrap.Imports.Events;

namespace TorrentSharp.Wrap.Notifications;

/// <summary>
/// Raised when a downloaded piece fails its hash check (corrupt or bad data from a peer) - the piece
/// is discarded and re-requested, never written to disk with bad content.
/// </summary>
public class HashFailedNotification(HashFailedEvent @event, TorrentManager torrentManager) : SessionNotification(@event.Info)
{
    public TorrentManager TorrentManager { get; } = torrentManager;

    public int PieceIndex { get; } = @event.PieceIndex;
}
