using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap.Imports.Events;

// session-wide, not associated with any particular torrent - no InfoHash field.
[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct SessionStatsEvent
{
    [MarshalAs(UnmanagedType.Struct)]
    public EventBase Info;

    public int Count;
    public IntPtr Values;
}
