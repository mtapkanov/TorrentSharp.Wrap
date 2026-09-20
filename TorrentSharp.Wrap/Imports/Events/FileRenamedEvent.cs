using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap.Imports.Events;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct FileRenamedEvent
{
    [MarshalAs(UnmanagedType.Struct)]
    public EventBase Info;

    public int FileIndex;

    [MarshalAs(UnmanagedType.U1)]
    public bool Succeeded;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public byte[] InfoHash;
}
