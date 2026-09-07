using System.Runtime.InteropServices;
using TorrentSharp.Wrap.Enums;

namespace TorrentSharp.Wrap.Imports.Events;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct TorrentStatusEvent
{
    [MarshalAs(UnmanagedType.Struct)]
    public EventBase Info;

    public TorrentState OldState;
    public TorrentState NewState;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public byte[] InfoHash;
}
