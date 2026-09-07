using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap.Imports;

internal static partial class Methods
{
    /// <summary>
    /// Schedules a piece for priority download, triggering a <see cref="Enums.NotificationType.ReadPiece"/>
    /// notification once it becomes available - whether freshly downloaded or already on disk.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <param name="pieceIndex">Index of the piece to prioritize</param>
    /// <param name="deadlineMs">Target completion time, in milliseconds from now</param>
    [LibraryImport(LibraryName, EntryPoint = "set_piece_deadline")]
    public static partial void SetPieceDeadline(IntPtr torrentSessionHandle, int pieceIndex, int deadlineMs);

    /// <summary>
    /// Clears a piece's deadline, dropping it back to normal download priority.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <param name="pieceIndex">Index of the piece to reset</param>
    [LibraryImport(LibraryName, EntryPoint = "reset_piece_deadline")]
    public static partial void ResetPieceDeadline(IntPtr torrentSessionHandle, int pieceIndex);

    /// <summary>
    /// Checks whether a piece has finished downloading and been written to disk.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <param name="pieceIndex">Index of the piece to check</param>
    [return: MarshalAs(UnmanagedType.I1)]
    [LibraryImport(LibraryName, EntryPoint = "have_piece")]
    public static partial bool HavePiece(IntPtr torrentSessionHandle, int pieceIndex);

    /// <summary>
    /// Reads the total piece count for a torrent.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    [LibraryImport(LibraryName, EntryPoint = "get_torrent_piece_count")]
    public static partial int GetTorrentPieceCount(IntPtr torrentSessionHandle);

    /// <summary>
    /// Fills <paramref name="piecesOut"/> with a one-byte-per-piece download map (0 or 1).
    /// Size the buffer using <see cref="GetTorrentPieceCount"/> first.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <param name="piecesOut">Buffer to populate</param>
    [LibraryImport(LibraryName, EntryPoint = "get_torrent_piece_map")]
    public static partial void GetTorrentPieceMap(IntPtr torrentSessionHandle, Span<byte> piecesOut, int piecesLen);

    /// <summary>
    /// Resolves a byte range within a file to the piece that contains it.
    /// </summary>
    /// <param name="torrentInfoHandle">Handle to an <c>lt::torrent_info</c>, from <see cref="Methods.GetHandleTorrentInfo"/></param>
    /// <param name="fileIndex">Index of the file the range belongs to</param>
    /// <param name="offset">Byte offset into the file where the range starts</param>
    /// <param name="size">Length of the range, in bytes</param>
    [LibraryImport(LibraryName, EntryPoint = "map_file_range")]
    public static partial PieceRequest MapFileRange(IntPtr torrentInfoHandle, int fileIndex, long offset, int size);
}
