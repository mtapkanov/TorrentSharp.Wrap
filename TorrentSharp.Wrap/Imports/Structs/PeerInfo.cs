using System.Runtime.InteropServices;
using TorrentSharp.Wrap.Enums;

namespace TorrentSharp.Wrap.Imports.Structs;

/// <summary>
/// A single connected peer, mirroring the data from torrent_handle::get_peer_info.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal readonly struct PeerInfo
{
    [MarshalAs(UnmanagedType.LPUTF8Str)]
    public readonly string Address;

    [MarshalAs(UnmanagedType.LPUTF8Str)]
    public readonly string Client;

    public readonly long TotalDownload;
    public readonly long TotalUpload;

    public readonly int DownloadRate;
    public readonly int UploadRate;

    public readonly PeerEncryptionType EncryptionType;
    public readonly PeerDirection Direction;

    [MarshalAs(UnmanagedType.I1)]
    public readonly bool IsSeed;

    [MarshalAs(UnmanagedType.I1)]
    public readonly bool WeAreChoking;

    [MarshalAs(UnmanagedType.I1)]
    public readonly bool TheyAreChoking;

    [MarshalAs(UnmanagedType.I1)]
    public readonly bool AmInterested;

    [MarshalAs(UnmanagedType.I1)]
    public readonly bool IsInterested;
}
