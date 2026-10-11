using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap.Imports.Events;

// session-wide, not associated with any particular torrent - no InfoHash field.
[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct ListenFailedEvent
{
    [MarshalAs(UnmanagedType.Struct)]
    public EventBase Info;

    public int ErrorValue;
    public byte Operation;
    public byte SocketType;

    public int Port;

    [MarshalAs(UnmanagedType.LPUTF8Str)]
    public string ListenInterface;
}
