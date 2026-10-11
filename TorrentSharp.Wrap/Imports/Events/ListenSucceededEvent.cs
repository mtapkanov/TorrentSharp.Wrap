using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap.Imports.Events;

// session-wide, not associated with any particular torrent - no InfoHash field.
[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct ListenSucceededEvent
{
    [MarshalAs(UnmanagedType.Struct)]
    public EventBase Info;

    public byte SocketType;
    public int Port;

    [MarshalAs(UnmanagedType.LPUTF8Str)]
    public string Address;
}
