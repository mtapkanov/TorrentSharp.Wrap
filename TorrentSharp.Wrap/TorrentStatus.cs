using System.Runtime.InteropServices;
using TorrentSharp.Wrap.Enums;

namespace TorrentSharp.Wrap;

/// <summary>
/// Snapshot of an attached torrent's current state.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 8)]
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

    /// <summary>Total bytes wanted (selected for download), including what's already downloaded.</summary>
    public readonly long TotalWanted;

    /// <summary>Bytes wanted that have already been downloaded.</summary>
    public readonly long TotalWantedDone;

    /// <summary>Total bytes uploaded across this torrent's entire lifetime, including previous sessions.</summary>
    public readonly long AllTimeUpload;

    /// <summary>Total bytes downloaded across this torrent's entire lifetime, including previous sessions.</summary>
    public readonly long AllTimeDownload;

    private readonly long _addedTime;

    /// <summary>
    /// Number of open connections to peers, including half-open ones still being established.
    /// </summary>
    public readonly int ConnectionCount;

    /// <summary>
    /// Position in the session's download queue - lower positions are downloaded first among
    /// non-seeding torrents. <c>-1</c> if the torrent isn't queued (e.g. it's seeding).
    /// </summary>
    public readonly int QueuePosition;

    /// <summary>
    /// The number of distinct, complete copies of the torrent currently distributed across the swarm.
    /// </summary>
    public readonly int DistributedFullCopies;

    // bool здесь не blittable для LibraryImport-параметра, передаваемого по значению/out (в отличие
    // от PeerInfo/TrackerInfo, которые никогда не идут напрямую в LibraryImport-сигнатуру, а
    // маршалятся вручную через Marshal.PtrToStructure) - поэтому храним как byte 0/1 и открываем
    // как bool через вычисляемое свойство.
    private readonly byte _isFinished;
    private readonly byte _movingStorage;

    /// <summary>Whether all selected (wanted) files have finished downloading.</summary>
    public bool IsFinished => _isFinished != 0;

    /// <summary>Whether this torrent's storage is currently being moved (see <c>MoveStorageAsync</c>).</summary>
    public bool MovingStorage => _movingStorage != 0;

    /// <summary>When this torrent was added to the session.</summary>
    public DateTimeOffset AddedAt => DateTimeOffset.FromUnixTimeSeconds(_addedTime);
}