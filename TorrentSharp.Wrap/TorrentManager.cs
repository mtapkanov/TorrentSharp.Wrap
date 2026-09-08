using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using TorrentSharp.Wrap.Imports;
using TorrentSharp.Wrap.Imports.Structs;

namespace TorrentSharp.Wrap;


public class TorrentManager
{
    internal readonly IntPtr TorrentSessionHandle;

    private readonly string _savePath;
    private readonly TaskCompletionSource? _metadataTaskSrc;

    // одновременно может быть несколько чтений кусков в полёте (например, скользящее окно
    // потокового чтения), поэтому это словарь по индексу куска, а не одно поле, как _metadataTaskSrc.
    private readonly ConcurrentDictionary<int, TaskCompletionSource<byte[]>> _pendingPieceReads = new();

    private bool _detached;
    private int _detachRequested;
    private IReadOnlyList<TorrentManagerFile>? _files;

    internal TorrentManager(IntPtr torrentSessionHandle, string savePath, string infoHash, TorrentInfo? info)
    {
        Info = info;
        InfoHash = infoHash;
        TorrentSessionHandle = torrentSessionHandle;

        _savePath = savePath;
        _metadataTaskSrc = info == null ? new TaskCompletionSource() : null;
    }

    /// <summary>
    /// The torrent's v1 info-hash - available immediately, even before a magnet link's metadata arrives.
    /// </summary>
    public string InfoHash { get; }

    /// <summary>
    /// Parsed torrent data.
    /// For magnet links this stays <c>null</c> until metadata has been fetched.
    /// </summary>
    public TorrentInfo? Info { get; private set; }

    /// <summary>
    /// Whether to pause the torrent automatically right after metadata arrives, giving the caller a window
    /// to set file priorities before content starts downloading. Defaults to <c>true</c>.
    /// (Only meaningful for magnet links - regular torrent files already have their metadata up front.)
    /// </summary>
    public bool PauseAfterMetadata { get; set; } = true;

    /// <summary>
    /// The torrent's files, each with extra properties like priority and on-disk path.
    /// Empty for magnet links whose metadata hasn't arrived yet.
    /// </summary>
    public IReadOnlyList<TorrentManagerFile> Files
    {
        get
        {
            if (Info == null)
                return [];

            return _files ??= [.. Info.Files.Select(fileInfo => new TorrentManagerFile(TorrentSessionHandle, _savePath, fileInfo))];
        }
    }

    /// <summary>
    /// Reads the torrent's current status.
    /// </summary>
    /// <remarks>
    /// Each call makes a fresh unmanaged round-trip - cache the result where you can.
    /// </remarks>
    public TorrentStatus GetCurrentStatus()
    {
        ObjectDisposedException.ThrowIf(_detached, this);
        Methods.GetTorrentStatus(TorrentSessionHandle, out var status);

        return status;
    }

    /// <summary>
    /// Fetches connection details for every peer currently connected to this torrent.
    /// </summary>
    /// <remarks>
    /// Each call makes a fresh unmanaged round-trip - cache the result where you can.
    /// </remarks>
    public IReadOnlyList<ConnectedPeer> GetPeers()
    {
        ObjectDisposedException.ThrowIf(_detached, this);

        Methods.GetTorrentPeers(TorrentSessionHandle, out var list);
        try
        {
            var result = new List<ConnectedPeer>(list.Length);
            var size = Marshal.SizeOf<PeerInfo>();

            for (var i = 0; i < list.Length; i++)
            {
                var entry = Marshal.PtrToStructure<PeerInfo>(list.Peers + i * size);
                result.Add(new ConnectedPeer
                {
                    Address = entry.Address,
                    Client = entry.Client,
                    TotalDownload = entry.TotalDownload,
                    TotalUpload = entry.TotalUpload,
                    DownloadRate = entry.DownloadRate,
                    UploadRate = entry.UploadRate,
                    EncryptionType = entry.EncryptionType,
                    Direction = entry.Direction,
                    IsSeed = entry.IsSeed,
                    WeAreChoking = entry.WeAreChoking,
                    TheyAreChoking = entry.TheyAreChoking,
                    AmInterested = entry.AmInterested,
                    IsInterested = entry.IsInterested
                });
            }

            return result;
        }
        finally
        {
            Methods.FreeTorrentPeerList(ref list);
        }
    }

    /// <summary>
    /// Fetches announce state for every tracker attached to this torrent, across all tiers.
    /// </summary>
    /// <remarks>
    /// Each call makes a fresh unmanaged round-trip - cache the result where you can.
    /// </remarks>
    public IReadOnlyList<ConnectedTracker> GetTrackers()
    {
        ObjectDisposedException.ThrowIf(_detached, this);

        Methods.GetTorrentTrackers(TorrentSessionHandle, out var list);
        try
        {
            var result = new List<ConnectedTracker>(list.Length);
            var size = Marshal.SizeOf<TrackerInfo>();

            for (var i = 0; i < list.Length; i++)
            {
                var entry = Marshal.PtrToStructure<TrackerInfo>(list.Trackers + i * size);
                result.Add(new ConnectedTracker
                {
                    Tier = entry.Tier,
                    Url = entry.Url,
                    Verified = entry.Verified,
                    Fails = entry.Fails,
                    Updating = entry.Updating,
                    WarningMessage = string.IsNullOrEmpty(entry.WarningMessage) ? null : entry.WarningMessage,
                    FailureMessage = string.IsNullOrEmpty(entry.FailureMessage) ? null : entry.FailureMessage
                });
            }

            return result;
        }
        finally
        {
            Methods.FreeTorrentTrackerList(ref list);
        }
    }

    /// <summary>
    /// Starts or resumes the torrent's transfer.
    /// </summary>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(_detached, this);
        Methods.StartTorrent(TorrentSessionHandle);
    }

    /// <summary>
    /// Pauses the torrent's transfer.
    /// </summary>
    public void Stop()
    {
        ObjectDisposedException.ThrowIf(_detached, this);
        Methods.StopTorrent(TorrentSessionHandle);
    }

    /// <summary>
    /// Waits until the torrent's metadata is available, bounded by a <see cref="CancellationToken"/>.
    /// </summary>
    /// <param name="cancellationToken">Cancels the wait</param>
    public async Task WaitForMetadata(CancellationToken cancellationToken)
    {
        var task = _metadataTaskSrc?.Task ?? Task.CompletedTask;
        await task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Forces a fresh announce to every tracker.
    /// </summary>
    /// <param name="interval">Delay between this call and the actual announce</param>
    /// <param name="force">Whether to bypass the usual cooldown between announces</param>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="interval"/> was negative</exception>
    public void ReannounceAllTrackers(TimeSpan interval, bool force = false)
    {
        if (Math.Sign((int)interval.TotalSeconds) == -1)
            throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be a positive value.");

        ObjectDisposedException.ThrowIf(_detached, this);
        Methods.ReannounceTorrent(TorrentSessionHandle, (int)interval.TotalSeconds, force);
    }

    /// <summary>
    /// Total piece count for the torrent. Needs metadata to be available.
    /// </summary>
    public int PieceCount
    {
        get
        {
            ObjectDisposedException.ThrowIf(_detached, this);
            return Methods.GetTorrentPieceCount(TorrentSessionHandle);
        }
    }

    /// <summary>
    /// Reads a per-piece download map, one byte per piece (<c>0</c> or <c>1</c>).
    /// </summary>
    public byte[] GetPieceMap()
    {
        ObjectDisposedException.ThrowIf(_detached, this);

        var pieces = new byte[PieceCount];
        Methods.GetTorrentPieceMap(TorrentSessionHandle, pieces, pieces.Length);

        return pieces;
    }

    /// <summary>
    /// Checks whether a piece has finished downloading and been written to disk.
    /// </summary>
    public bool HavePiece(int pieceIndex)
    {
        ObjectDisposedException.ThrowIf(_detached, this);
        return Methods.HavePiece(TorrentSessionHandle, pieceIndex);
    }

    /// <summary>
    /// Schedules a piece for priority download - libtorrent favours pieces with a nearer deadline.
    /// <see cref="ReadPieceAsync"/> already does this for you if you just want to be notified once
    /// the piece is ready.
    /// </summary>
    public void SetPieceDeadline(int pieceIndex, int deadlineMs)
    {
        ObjectDisposedException.ThrowIf(_detached, this);
        Methods.SetPieceDeadline(TorrentSessionHandle, pieceIndex, deadlineMs);
    }

    /// <summary>
    /// Clears a piece's deadline, dropping it back to normal download priority.
    /// </summary>
    public void ResetPieceDeadline(int pieceIndex)
    {
        ObjectDisposedException.ThrowIf(_detached, this);
        Methods.ResetPieceDeadline(TorrentSessionHandle, pieceIndex);
    }

    /// <summary>
    /// Resolves a byte range within a file to the piece that contains it. Needs metadata to be available.
    /// </summary>
    public PieceRequest MapFileRange(int fileIndex, long offset, int size)
    {
        ObjectDisposedException.ThrowIf(_detached, this);

        return Info == null 
            ? throw new InvalidOperationException("Torrent metadata has not been received yet.") 
            : Methods.MapFileRange(Info.InfoHandle, fileIndex, offset, size);
    }

    /// <summary>
    /// Prioritizes a piece for download and asynchronously waits for its bytes.
    /// If the piece is already on disk, its data comes back right away - the same underlying
    /// event fires for a fresh download and for a piece that was already complete.
    /// </summary>
    /// <param name="pieceIndex">Index of the piece to read</param>
    /// <param name="cancellationToken">Cancels the wait; the underlying download keeps going regardless</param>
    /// <param name="timeout">
    /// Upper bound on the wait, independent of <paramref name="cancellationToken"/>.
    /// Without this, seeking into a not-yet-downloaded part of a torrent (or missing a
    /// read_piece_alert - libtorrent's set_alert_notify only re-fires on an empty-to-non-empty
    /// queue transition, not for every alert added while a batch is already draining) would hang
    /// this call forever, since nothing else would ever complete the pending read. Defaults to 30s.
    /// </param>
    /// <exception cref="IOException">The piece failed to read from the disk.</exception>
    /// <exception cref="TimeoutException">The piece didn't arrive within <paramref name="timeout"/></exception>
    public async Task<byte[]> ReadPieceAsync(int pieceIndex, CancellationToken cancellationToken, TimeSpan? timeout = null)
    {
        ObjectDisposedException.ThrowIf(_detached, this);

        var taskCompletionSource = _pendingPieceReads.GetOrAdd(
            pieceIndex,
            static _ => new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously));

        SetPieceDeadline(pieceIndex, 0);

        try
        {
            return await taskCompletionSource.Task.WaitAsync(timeout ?? TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _pendingPieceReads.TryRemove(new KeyValuePair<int, TaskCompletionSource<byte[]>>(pieceIndex, taskCompletionSource));
        }
    }

    /// <summary>
    /// Opens a read-only stream over one of this torrent's files, prioritizing pieces as they're
    /// read so sequential/seek-driven access (e.g. serving the file over HTTP) stays smooth
    /// without downloading the rest of the file eagerly.
    /// </summary>
    /// <param name="fileIndex">Index of the file to stream, matching its position in <see cref="Files"/></param>
    /// <param name="pieceTimeout">
    /// Upper bound on waiting for any single piece the stream needs to read - see
    /// <see cref="ReadPieceAsync"/>'s own <c>timeout</c> parameter. Defaults to 30s.
    /// </param>
    /// <exception cref="InvalidOperationException">Torrent metadata has not been received yet</exception>
    public Stream OpenFileStream(int fileIndex, TimeSpan? pieceTimeout = null)
    {
        ObjectDisposedException.ThrowIf(_detached, this);

        if (Info == null)
            throw new InvalidOperationException("Torrent metadata has not been received yet.");

        if (fileIndex < 0 || fileIndex >= Files.Count)
            throw new ArgumentOutOfRangeException(nameof(fileIndex));

        // каждый вызов возвращает независимый поток со своим локом и кэшем куска - конкурентные
        // стримы (даже на один и тот же файл) не блокируют и не вытесняют кэш друг друга.
        return new TorrentFileStream(this, Files[fileIndex], pieceTimeout);
    }

    // завершает ожидающий вызов ReadPieceAsync, как только кусок прочитан с диска.
    internal void OnPieceRead(int pieceIndex, byte[] data, bool succeeded)
    {
        if (!_pendingPieceReads.TryGetValue(pieceIndex, out var tcs))
            return;

        if (succeeded)
        {
            tcs.TrySetResult(data);
        }
        else
        {
            tcs.TrySetException(new IOException($"Failed to read piece {pieceIndex} from disk."));
        }
    }

    // сохраняет метаданные торрента и сигнализирует подписчикам, что они доступны.
    internal void OnMetadataReceived(TorrentInfo info)
    {
        Info = info;

        _files = null;
        _metadataTaskSrc?.TrySetResult();
    }

    // помечает объект как отсоединённый, делая его дальнейшее использование недопустимым.
    internal void MarkAsDetached()
    {
        _detached = true;
    }

    // TorrentClient убирает этот manager из своего словаря присоединённых торрентов только после
    // того, как асинхронно придёт уведомление TorrentRemoved - повторный вызов DetachTorrent до
    // этого момента иначе достучался бы до нативного detach_torrent второй раз на уже освобождённом
    // torrent_handle. Так первый вызвавший поток гарантированно побеждает, независимо от таймингов уведомления.
    internal bool TryMarkDetachRequested() => Interlocked.CompareExchange(ref _detachRequested, 1, 0) == 0;
}
