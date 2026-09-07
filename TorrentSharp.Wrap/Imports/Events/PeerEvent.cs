using System.Runtime.InteropServices;
using TorrentSharp.Wrap.Enums;

namespace TorrentSharp.Wrap.Imports.Events;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct PeerEvent
{
    [MarshalAs(UnmanagedType.Struct)]
    public EventBase Info;

    public IntPtr Handle;
    public PeerNotificationType NotificationType;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public byte[] InfoHash;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public byte[] V6Address;
}
