using System.Runtime.InteropServices;
using TorrentSharp.Wrap.Enums;

namespace TorrentSharp.Wrap.Imports.Events;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct PerformanceWarningEvent
{
    [MarshalAs(UnmanagedType.Struct)]
    public EventBase Info;

    public PerformanceWarningType WarningCode;
}
