using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap;

/// <summary>
/// Outcome of resolving a byte range within a file to the piece that contains it.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct PieceRequest
{
    /// <summary>
    /// Index of the piece where the range begins, or <c>-1</c> if resolution failed.
    /// </summary>
    public readonly int Piece;

    /// <summary>
    /// Byte offset into <see cref="Piece"/> at which the range begins.
    /// </summary>
    public readonly int Offset;

    /// <summary>
    /// Length of the range, in bytes.
    /// </summary>
    public readonly int Length;
}
