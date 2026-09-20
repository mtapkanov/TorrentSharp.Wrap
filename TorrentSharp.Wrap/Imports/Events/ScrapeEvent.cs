using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap.Imports.Events;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct ScrapeEvent
{
    [MarshalAs(UnmanagedType.Struct)]
    public EventBase Info;

    [MarshalAs(UnmanagedType.U1)]
    public bool Succeeded;

    public int Incomplete;
    public int Complete;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public byte[] InfoHash;
}
