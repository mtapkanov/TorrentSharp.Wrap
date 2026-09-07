using System.Runtime.InteropServices;
using TorrentSharp.Wrap.Enums;

namespace TorrentSharp.Wrap;

/// <summary>
/// Snapshot of an attached torrent's current state.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct TorrentStatus
{
    public readonly TorrentState State;
    public readonly float Progress;

    public readonly int PeerCount;
    public readonly int SeedCount;

    public readonly long BytesUploaded;
    public readonly long BytesDownloaded;

    public readonly long UploadRate;
    public readonly long DownloadRate;
}