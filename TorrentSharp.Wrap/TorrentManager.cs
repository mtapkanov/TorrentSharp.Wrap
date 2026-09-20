using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using TorrentSharp.Wrap.Enums;
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

    // переименования разных файлов торрента независимы друг от друга, поэтому тоже словарь -
    // по аналогии с _pendingPieceReads, а не одно поле.
    private readonly ConcurrentDictionary<int, TaskCompletionSource<bool>> _pendingFileRenames = new();

    // move_storage у libtorrent не принимает пользовательский токен запроса, так что одновременно
    // может быть только один ожидающий вызов MoveStorageAsync - как с _metadataTaskSrc.
    private TaskCompletionSource<bool>? _pendingStorageMove;

    // scrape_reply_alert/scrape_failed_alert не несут в себе индекс трекера, который был
    // заскрейплен - только его URL, а на URL мы здесь не завязываемся - поэтому, как и с
    // _pendingStorageMove, одновременно может быть только один ожидающий вызов ScrapeTrackerAsync.
    private TaskCompletionSource<(int Incomplete, int Complete)>? _pendingScrape;

    // save_resume_data у libtorrent тоже не принимает пользовательский токен запроса, так что
    // одновременно может быть только один ожидающий вызов SaveResumeDataAsync - как с _pendingStorageMove.
    private TaskCompletionSource<byte[]>? _pendingResumeDataSave;

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
    /// Adds a tracker to this torrent's announce list.
    /// </summary>
    /// <param name="url">Tracker announce URL</param>
    /// <param name="tier">Tier the tracker belongs to - lower tiers are tried first</param>
    public void AddTracker(string url, byte tier = 0)
    {
        ObjectDisposedException.ThrowIf(_detached, this);
        Methods.AddTorrentTracker(TorrentSessionHandle, url, tier);
    }

    /// <summary>
    /// Replaces this torrent's entire announce list.
    /// </summary>
    /// <param name="trackers">New set of (url, tier) pairs - lower tiers are tried first</param>
    public void ReplaceTrackers(IReadOnlyList<(string Url, byte Tier)> trackers)
    {
        ObjectDisposedException.ThrowIf(_detached, this);

        var urls = new string[trackers.Count];
        var tiers = new byte[trackers.Count];

        for (var i = 0; i < trackers.Count; i++)
        {
            urls[i] = trackers[i].Url;
            tiers[i] = trackers[i].Tier;
        }

        Methods.ReplaceTorrentTrackers(TorrentSessionHandle, urls, tiers, trackers.Count);
    }

    /// <summary>
    /// Adds a BEP 19 (GetRight-style) web seed to this torrent.
    /// </summary>
    public void AddUrlSeed(string url)
    {
        ObjectDisposedException.ThrowIf(_detached, this);
        Methods.AddTorrentUrlSeed(TorrentSessionHandle, url);
    }

    /// <summary>
    /// Adds a BEP 17 (Hoffman-style) web seed to this torrent.
    /// </summary>
    public void AddHttpSeed(string url)
    {
        ObjectDisposedException.ThrowIf(_detached, this);
        Methods.AddTorrentHttpSeed(TorrentSessionHandle, url);
    }

    /// <summary>
    /// Sends a scrape request to a tracker and asynchronously waits for the result.
    /// </summary>
    /// <remarks>
    /// Only one scrape can be pending at a time - libtorrent's scrape alerts don't carry back which
    /// tracker index was scraped (only its URL, which this doesn't key on), so starting a new scrape
    /// while one is already pending abandons the wait for the first (its own <paramref name="timeout"/>
    /// still applies; the scrape itself isn't cancelled).
    /// </remarks>
    /// <param name="trackerIndex">Index of the tracker to scrape, or <c>-1</c> for the last working tracker</param>
    /// <param name="cancellationToken">Cancels the wait; the scrape itself keeps going regardless</param>
    /// <param name="timeout">Upper bound on the wait, independent of <paramref name="cancellationToken"/>. Defaults to 30s.</param>
    /// <returns>Peer counts reported by the tracker</returns>
    /// <exception cref="IOException">The scrape request failed.</exception>
    /// <exception cref="TimeoutException">No result arrived within <paramref name="timeout"/></exception>
    public async Task<(int Incomplete, int Complete)> ScrapeTrackerAsync(int trackerIndex, CancellationToken cancellationToken, TimeSpan? timeout = null)
    {
        ObjectDisposedException.ThrowIf(_detached, this);

        var taskCompletionSource = new TaskCompletionSource<(int, int)>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingScrape = taskCompletionSource;

        Methods.ScrapeTorrentTracker(TorrentSessionHandle, trackerIndex);

        return await taskCompletionSource.Task.WaitAsync(timeout ?? TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
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
    /// This torrent's upload bandwidth limit, in bytes/sec. <c>0</c> means unlimited.
    /// </summary>
    public int UploadLimit
    {
        get
        {
            ObjectDisposedException.ThrowIf(_detached, this);
            return Methods.GetTorrentUploadLimit(TorrentSessionHandle);
        }
        set
        {
            ObjectDisposedException.ThrowIf(_detached, this);
            Methods.SetTorrentUploadLimit(TorrentSessionHandle, value);
        }
    }

    /// <summary>
    /// This torrent's download bandwidth limit, in bytes/sec. <c>0</c> means unlimited.
    /// </summary>
    public int DownloadLimit
    {
        get
        {
            ObjectDisposedException.ThrowIf(_detached, this);
            return Methods.GetTorrentDownloadLimit(TorrentSessionHandle);
        }
        set
        {
            ObjectDisposedException.ThrowIf(_detached, this);
            Methods.SetTorrentDownloadLimit(TorrentSessionHandle, value);
        }
    }

    /// <summary>
    /// This torrent's position in the session's download queue - lower positions are downloaded
    /// first among non-seeding torrents. <c>-1</c> if the torrent isn't queued.
    /// </summary>
    public int QueuePosition
    {
        get
        {
            ObjectDisposedException.ThrowIf(_detached, this);
            return Methods.GetTorrentQueuePosition(TorrentSessionHandle);
        }
    }

    /// <summary>
    /// Moves this torrent one position up (earlier) in the download queue.
    /// </summary>
    public void MoveQueueUp()
    {
        ObjectDisposedException.ThrowIf(_detached, this);
        Methods.TorrentQueuePositionUp(TorrentSessionHandle);
    }

    /// <summary>
    /// Moves this torrent one position down (later) in the download queue.
    /// </summary>
    public void MoveQueueDown()
    {
        ObjectDisposedException.ThrowIf(_detached, this);
        Methods.TorrentQueuePositionDown(TorrentSessionHandle);
    }

    /// <summary>
    /// Moves this torrent to the top of the download queue.
    /// </summary>
    public void MoveQueueToTop()
    {
        ObjectDisposedException.ThrowIf(_detached, this);
        Methods.TorrentQueuePositionTop(TorrentSessionHandle);
    }

    /// <summary>
    /// Moves this torrent to the bottom of the download queue.
    /// </summary>
    public void MoveQueueToBottom()
    {
        ObjectDisposedException.ThrowIf(_detached, this);
        Methods.TorrentQueuePositionBottom(TorrentSessionHandle);
    }

    /// <summary>
    /// Whether this torrent downloads pieces roughly in order, rather than rarest-first.
    /// Useful for sequential playback; <see cref="OpenFileStream"/> manages this on its own via
    /// piece deadlines and doesn't need this flag set.
    /// </summary>
    public bool SequentialDownload
    {
        get
        {
            ObjectDisposedException.ThrowIf(_detached, this);
            return Methods.GetTorrentSequentialDownload(TorrentSessionHandle);
        }
        set
        {
            ObjectDisposedException.ThrowIf(_detached, this);
            Methods.SetTorrentSequentialDownload(TorrentSessionHandle, value);
        }
    }

    /// <summary>
    /// Whether this finished torrent is in super-seeding mode, which distributes pieces to
    /// maximize their spread across the swarm instead of maximizing this client's upload rate.
    /// </summary>
    public bool SuperSeeding
    {
        get
        {
            ObjectDisposedException.ThrowIf(_detached, this);
            return Methods.GetTorrentSuperSeeding(TorrentSessionHandle);
        }
        set
        {
            ObjectDisposedException.ThrowIf(_detached, this);
            Methods.SetTorrentSuperSeeding(TorrentSessionHandle, value);
        }
    }

    /// <summary>
    /// Whether this torrent is in share mode, which optimizes for improving the swarm's overall
    /// ratio rather than completing the download quickly.
    /// </summary>
    public bool ShareMode
    {
        get
        {
            ObjectDisposedException.ThrowIf(_detached, this);
            return Methods.GetTorrentShareMode(TorrentSessionHandle);
        }
        set
        {
            ObjectDisposedException.ThrowIf(_detached, this);
            Methods.SetTorrentShareMode(TorrentSessionHandle, value);
        }
    }

    /// <summary>
    /// Whether this torrent only uploads and never downloads, even if it's incomplete.
    /// </summary>
    public bool UploadMode
    {
        get
        {
            ObjectDisposedException.ThrowIf(_detached, this);
            return Methods.GetTorrentUploadMode(TorrentSessionHandle);
        }
        set
        {
            ObjectDisposedException.ThrowIf(_detached, this);
            Methods.SetTorrentUploadMode(TorrentSessionHandle, value);
        }
    }

    /// <summary>
    /// Triggers a full recheck of this torrent's data on disk.
    /// </summary>
    /// <remarks>
    /// Progress is visible through <see cref="GetCurrentStatus"/>'s <c>Checking</c>/
    /// <c>CheckingResume</c> states, not through a dedicated notification.
    /// </remarks>
    public void ForceRecheck()
    {
        ObjectDisposedException.ThrowIf(_detached, this);
        Methods.ForceRecheck(TorrentSessionHandle);
    }

    /// <summary>
    /// Renames one of this torrent's files and asynchronously waits for the result.
    /// </summary>
    /// <param name="fileIndex">Index of the file to rename, matching its position in <see cref="Files"/></param>
    /// <param name="newName">New name for the file</param>
    /// <param name="cancellationToken">Cancels the wait; the rename itself keeps going regardless</param>
    /// <param name="timeout">Upper bound on the wait, independent of <paramref name="cancellationToken"/>. Defaults to 30s.</param>
    /// <exception cref="IOException">The file failed to rename on disk.</exception>
    /// <exception cref="TimeoutException">No result arrived within <paramref name="timeout"/></exception>
    public async Task RenameFileAsync(int fileIndex, string newName, CancellationToken cancellationToken, TimeSpan? timeout = null)
    {
        ObjectDisposedException.ThrowIf(_detached, this);

        var taskCompletionSource = _pendingFileRenames.GetOrAdd(
            fileIndex,
            static _ => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));

        Methods.RenameTorrentFile(TorrentSessionHandle, fileIndex, newName);

        try
        {
            var succeeded = await taskCompletionSource.Task.WaitAsync(timeout ?? TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);

            if (!succeeded)
                throw new IOException($"Failed to rename file {fileIndex}.");
        }
        finally
        {
            _pendingFileRenames.TryRemove(new KeyValuePair<int, TaskCompletionSource<bool>>(fileIndex, taskCompletionSource));
        }
    }

    /// <summary>
    /// Moves this torrent's storage to a new path and asynchronously waits for the result.
    /// </summary>
    /// <remarks>
    /// Only one move can be pending at a time - libtorrent's <c>move_storage</c> doesn't accept a
    /// caller-supplied token, so starting a new move while one is already pending abandons the
    /// wait for the first (its own <paramref name="timeout"/> still applies; the move itself isn't
    /// cancelled).
    /// </remarks>
    /// <param name="newPath">Destination directory for the torrent's contents</param>
    /// <param name="cancellationToken">Cancels the wait; the move itself keeps going regardless</param>
    /// <param name="timeout">Upper bound on the wait, independent of <paramref name="cancellationToken"/>. Defaults to 30s.</param>
    /// <exception cref="IOException">The storage failed to move.</exception>
    /// <exception cref="TimeoutException">No result arrived within <paramref name="timeout"/></exception>
    public async Task MoveStorageAsync(string newPath, CancellationToken cancellationToken, TimeSpan? timeout = null)
    {
        ObjectDisposedException.ThrowIf(_detached, this);

        var taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingStorageMove = taskCompletionSource;

        Methods.MoveTorrentStorage(TorrentSessionHandle, newPath);

        var succeeded = await taskCompletionSource.Task.WaitAsync(timeout ?? TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);

        if (!succeeded)
            throw new IOException("Failed to move torrent storage.");
    }

    /// <summary>
    /// Saves this torrent's resume data (piece/file state, trackers, and metadata once available)
    /// and asynchronously waits for the result. Pass the returned bytes back to
    /// <see cref="TorrentClient.AttachTorrent"/>/<see cref="TorrentClient.AttachMagnet"/> on a later
    /// attach to skip a full recheck of data already on disk.
    /// </summary>
    /// <remarks>
    /// Only one save can be pending at a time - libtorrent's <c>save_resume_data</c> doesn't accept
    /// a caller-supplied token, so starting a new save while one is already pending abandons the
    /// wait for the first (its own <paramref name="timeout"/> still applies; the save itself isn't
    /// cancelled).
    /// </remarks>
    /// <param name="cancellationToken">Cancels the wait; the save itself keeps going regardless</param>
    /// <param name="timeout">Upper bound on the wait, independent of <paramref name="cancellationToken"/>. Defaults to 30s.</param>
    /// <returns>The bencoded resume data</returns>
    /// <exception cref="IOException">The resume data failed to save.</exception>
    /// <exception cref="TimeoutException">No result arrived within <paramref name="timeout"/></exception>
    public async Task<byte[]> SaveResumeDataAsync(CancellationToken cancellationToken, TimeSpan? timeout = null)
    {
        ObjectDisposedException.ThrowIf(_detached, this);

        var taskCompletionSource = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingResumeDataSave = taskCompletionSource;

        Methods.SaveTorrentResumeData(TorrentSessionHandle);

        return await taskCompletionSource.Task.WaitAsync(timeout ?? TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
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
    /// Reads the download priority of a single piece.
    /// </summary>
    public FileDownloadPriority GetPiecePriority(int pieceIndex)
    {
        ObjectDisposedException.ThrowIf(_detached, this);
        return Methods.GetPiecePriority(TorrentSessionHandle, pieceIndex);
    }

    /// <summary>
    /// Sets the download priority of a single piece.
    /// </summary>
    public void SetPiecePriority(int pieceIndex, FileDownloadPriority priority)
    {
        ObjectDisposedException.ThrowIf(_detached, this);
        Methods.SetPiecePriority(TorrentSessionHandle, pieceIndex, priority);
    }

    /// <summary>
    /// Reads the download priority of every piece in the torrent.
    /// </summary>
    public FileDownloadPriority[] GetPiecePriorities()
    {
        ObjectDisposedException.ThrowIf(_detached, this);

        var priorities = new FileDownloadPriority[PieceCount];
        Methods.GetTorrentPiecePriorities(TorrentSessionHandle, priorities, priorities.Length);

        return priorities;
    }

    /// <summary>
    /// Sets the download priority of every piece in the torrent at once.
    /// </summary>
    /// <param name="priorities">One priority per piece, matching <see cref="PieceCount"/></param>
    public void SetPiecePriorities(FileDownloadPriority[] priorities)
    {
        ObjectDisposedException.ThrowIf(_detached, this);
        Methods.SetTorrentPiecePriorities(TorrentSessionHandle, priorities, priorities.Length);
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

    // завершает ожидающий вызов RenameFileAsync для этого файла, если он есть.
    internal void OnFileRenamed(int fileIndex, bool succeeded)
    {
        if (_pendingFileRenames.TryGetValue(fileIndex, out var tcs))
            tcs.TrySetResult(succeeded);
    }

    // завершает ожидающий вызов MoveStorageAsync, если он есть.
    internal void OnStorageMoved(bool succeeded)
    {
        _pendingStorageMove?.TrySetResult(succeeded);
    }

    // завершает ожидающий вызов ScrapeTrackerAsync, если он есть.
    internal void OnScrapeCompleted(bool succeeded, int incomplete, int complete)
    {
        if (succeeded)
        {
            _pendingScrape?.TrySetResult((incomplete, complete));
        }
        else
        {
            _pendingScrape?.TrySetException(new IOException("Scrape request failed."));
        }
    }

    // завершает ожидающий вызов SaveResumeDataAsync, если он есть.
    internal void OnResumeDataSaved(bool succeeded, byte[] data)
    {
        if (succeeded)
        {
            _pendingResumeDataSave?.TrySetResult(data);
        }
        else
        {
            _pendingResumeDataSave?.TrySetException(new IOException("Failed to save resume data."));
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

    // true с момента, когда TorrentClient.DetachTorrent начал отсоединение (до того, как придёт
    // асинхронное уведомление TorrentRemoved) - позволяет коду обработки алертов пропустить любой
    // дальнейший нативный вызов на этом handle, а не только внешним вызывающим полагаться на
    // ObjectDisposedException.
    internal bool IsDetached => _detached;

    // TorrentClient убирает этот manager из своего словаря присоединённых торрентов только после
    // того, как асинхронно придёт уведомление TorrentRemoved - повторный вызов DetachTorrent до
    // этого момента иначе достучался бы до нативного detach_torrent второй раз на уже освобождённом
    // torrent_handle. Так первый вызвавший поток гарантированно побеждает, независимо от таймингов уведомления.
    internal bool TryMarkDetachRequested() => Interlocked.CompareExchange(ref _detachRequested, 1, 0) == 0;
}
