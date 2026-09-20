using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap.Imports.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal readonly struct SessionStatsMetricList
{
    public readonly int Length;
    public readonly IntPtr Metrics;
}
