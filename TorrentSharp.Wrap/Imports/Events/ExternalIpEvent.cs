using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap.Imports.Events;

// session-wide, not associated with any particular torrent - no InfoHash field.
[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct ExternalIpEvent
{
    [MarshalAs(UnmanagedType.Struct)]
    public EventBase Info;

    [MarshalAs(UnmanagedType.LPUTF8Str)]
    public string ExternalAddress;
}
