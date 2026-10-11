using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap.Imports.Events;

// session-wide, not associated with any particular torrent - no InfoHash field.
[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct PortmapErrorEvent
{
    [MarshalAs(UnmanagedType.Struct)]
    public EventBase Info;

    public int ErrorValue;
    public byte Transport;
}
