using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap.Imports.Events;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct FileErrorEvent
{
    [MarshalAs(UnmanagedType.Struct)]
    public EventBase Info;

    public int ErrorValue;
    public byte Operation;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public byte[] InfoHash;

    [MarshalAs(UnmanagedType.LPUTF8Str)]
    public string Filename;
}
