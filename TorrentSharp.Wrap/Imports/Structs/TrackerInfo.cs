using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap.Imports.Structs;

/// <summary>
/// Announce state for a single tracker, mirroring the data from torrent_handle::trackers.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal readonly struct TrackerInfo
{
    public readonly int Tier;

    [MarshalAs(UnmanagedType.LPUTF8Str)]
    public readonly string Url;

    [MarshalAs(UnmanagedType.I1)]
    public readonly bool Verified;

    public readonly byte Fails;

    [MarshalAs(UnmanagedType.I1)]
    public readonly bool Updating;

    [MarshalAs(UnmanagedType.LPUTF8Str)]
    public readonly string WarningMessage;

    [MarshalAs(UnmanagedType.LPUTF8Str)]
    public readonly string FailureMessage;
}
