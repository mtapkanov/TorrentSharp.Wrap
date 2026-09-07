using System.Runtime.InteropServices;
using TorrentSharp.Wrap.Enums;

namespace TorrentSharp.Wrap.Imports.Events;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct EventBase
{
    [MarshalAs(UnmanagedType.I4)]
    public NotificationType Type;

    public int Category;
    public long Timestamp;

    [MarshalAs(UnmanagedType.LPUTF8Str)]
    public string Message;
}
