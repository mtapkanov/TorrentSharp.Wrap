using TorrentSharp.Wrap.Imports.Events;

namespace TorrentSharp.Wrap.Notifications;

public class ReadPieceNotification(ReadPieceEvent @event, TorrentManager torrentManager, byte[] buffer) : SessionNotification(@event.Info)
{
    public TorrentManager TorrentManager { get; } = torrentManager;

    /// <summary>
    /// Index of the piece that was read.
    /// </summary>
    public int Piece { get; } = @event.Piece;

    /// <summary>
    /// Length of <see cref="Buffer"/>, in bytes; <c>0</c> when <see cref="Succeeded"/> is <c>false</c>.
    /// </summary>
    public int Size { get; } = @event.Size;

    /// <summary>
    /// Whether the read completed successfully.
    /// </summary>
    public bool Succeeded { get; } = @event.Succeeded;

    /// <summary>
    /// The piece's data, or an empty array when <see cref="Succeeded"/> is <c>false</c>.
    /// </summary>
    public byte[] Buffer { get; } = buffer;
}
