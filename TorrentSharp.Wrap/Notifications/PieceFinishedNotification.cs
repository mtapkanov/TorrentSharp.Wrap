using TorrentSharp.Wrap.Imports.Events;

namespace TorrentSharp.Wrap.Notifications;

/// <summary>
/// Raised every time a piece finishes downloading and passes its hash check. May fire before the
/// piece is fully flushed to disk - <see cref="TorrentManager.HavePiece"/> can still return false
/// right after this.
/// </summary>
public class PieceFinishedNotification(PieceFinishedEvent @event, TorrentManager torrentManager) : SessionNotification(@event.Info)
{
    public TorrentManager TorrentManager { get; } = torrentManager;

    public int PieceIndex { get; } = @event.PieceIndex;
}
