using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap.Imports.Structs;

/// <summary>
/// A native array of trackers.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal readonly struct TrackerList
{
    public readonly int Length;
    public readonly IntPtr Trackers;
}
