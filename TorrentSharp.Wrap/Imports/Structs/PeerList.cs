using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap.Imports.Structs;

/// <summary>
/// A native array of connected peers.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal readonly struct PeerList
{
    public readonly int Length;
    public readonly IntPtr Peers;
}
