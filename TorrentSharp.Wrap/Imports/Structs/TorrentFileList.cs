using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap.Imports.Structs;

/// <summary>
/// A native array of a torrent's file entries.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal readonly struct TorrentFileList
{
    public readonly int Length;
    public readonly IntPtr Items;
}
