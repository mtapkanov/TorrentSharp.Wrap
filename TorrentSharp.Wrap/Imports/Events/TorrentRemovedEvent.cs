using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap.Imports.Events;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct TorrentRemovedEvent
{
    [MarshalAs(UnmanagedType.Struct)]
    public EventBase Info;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public byte[] InfoHash;
}
