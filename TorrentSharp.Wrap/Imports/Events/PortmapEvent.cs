using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap.Imports.Events;

// session-wide, not associated with any particular torrent - no InfoHash field.
[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct PortmapEvent
{
    [MarshalAs(UnmanagedType.Struct)]
    public EventBase Info;

    public int ExternalPort;
    public byte Transport;
}
