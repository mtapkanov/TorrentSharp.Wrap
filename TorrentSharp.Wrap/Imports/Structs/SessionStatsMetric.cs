using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap.Imports.Structs;

/// <summary>
/// A single entry from lt::session_stats_metrics() - a name/index pair describing a slot in the
/// counters array carried by a session stats notification.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal readonly struct SessionStatsMetric
{
    [MarshalAs(UnmanagedType.LPUTF8Str)]
    public readonly string Name;

    public readonly int ValueIndex;

    public readonly byte Type;
}
