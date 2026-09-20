using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap.Imports.Events;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct ResumeDataEvent
{
    [MarshalAs(UnmanagedType.Struct)]
    public EventBase Info;

    [MarshalAs(UnmanagedType.U1)]
    public bool Succeeded;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public byte[] InfoHash;

    public int Size;
    public IntPtr Buffer;
}
