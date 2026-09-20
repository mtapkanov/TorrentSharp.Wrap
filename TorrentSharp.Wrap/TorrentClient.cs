using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using TorrentSharp.Wrap.Configurations;
using TorrentSharp.Wrap.Configurations.Settings;
using TorrentSharp.Wrap.Notifications;
using TorrentSharp.Wrap.Enums;
using TorrentSharp.Wrap.Imports.Events;
using TorrentSharp.Wrap.Imports.Structs;
using Methods = TorrentSharp.Wrap.Imports.Methods;

namespace TorrentSharp.Wrap;

/// <summary>
/// A client that manages a libtorrent session and its attached torrents.
/// </summary>
public class TorrentClient : IDisposable
{
    // Storage нужна для read_piece_alert (ReadPiece) - без неё libtorrent никогда не выдаст
    // этот event, независимо от того, как выставлены дедлайны. Tracker аналогично нужна для
    // scrape_reply_alert/scrape_failed_alert (Scrape), которые ScrapeTrackerAsync ждёт напрямую.
    private const NotificationCategories RequiredNotificationCategories = NotificationCategories.Status | NotificationCategories.Storage | NotificationCategories.Tracker;

    private readonly ConcurrentDictionary<string, TorrentManager> _attachedManagers = new(StringComparer.OrdinalIgnoreCase);

    // ссылку на делегате нужно держать явно, иначе GC может собрать её раньше времени
    private readonly Methods.SessionEventCallback _eventCallback;
    private readonly IntPtr _handle;

    // post_session_stats у libtorrent не принимает пользовательский токен запроса, так что
    // одновременно может быть только один ожидающий вызов GetSessionStatsAsync - как с
    // TorrentManager._pendingStorageMove.
    private TaskCompletionSource<IReadOnlyDictionary<string, long>>? _pendingSessionStats;

    // таблица имя->индекс метрик статистики сессии не зависит от конкретного экземпляра сессии
    // (одна и та же для всего процесса на данной версии libtorrent) - получаем один раз и кэшируем.
    private static IReadOnlyList<(string Name, int ValueIndex)>? _sessionStatsMetricTable;

    private bool _disposed;
    private bool _includeUnmappedEvents;

    /// <summary>
    /// Creates a client using default settings.
    /// </summary>
    public TorrentClient() : this(new SettingsPack())
    {
    }

    /// <summary>
    /// Creates a client from a high-level configuration object.
    /// </summary>
    public TorrentClient(TorrentClientConfig config) : this(config.Build())
    {
    }

    /// <summary>
    /// Shared implementation the public constructors funnel through.
    /// For settings beyond what <see cref="TorrentClientConfig"/> exposes, construct normally
    /// and call <see cref="UpdateSettings"/> afterward.
    /// </summary>
    private unsafe TorrentClient(SettingsPack pack)
    {
        ValidateSettingsPack(pack);

        var packHandle = pack.BuildNative();

        try
        {
            _handle = Methods.CreateSession(packHandle.ToPointer());

            if (_handle == IntPtr.Zero)
                throw new InvalidOperationException("Failed to create session.");

            _eventCallback = ProxyRaisedEvent;
            Methods.SetEventCallback(_handle, _eventCallback, _includeUnmappedEvents);
        }
        finally
        {
            Methods.FreeSettingsPack(packHandle);
        }
    }

    ~TorrentClient()
    {
        Dispose();
    }

    /// <summary>
    /// Torrents currently attached to this session.
    /// </summary>
    public IEnumerable<TorrentManager> ActiveTorrents => _attachedManagers.Values;

    /// <summary>
    /// Whether to also raise a bare <see cref="SessionNotification"/> for event types with no dedicated payload.
    /// </summary>
    /// <remarks>
    /// Changing this after the client is already listening resets the underlying event callback.
    /// </remarks>
    public bool IncludeUnmappedEvents
    {
        get => _includeUnmappedEvents;
        set
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _includeUnmappedEvents = value;

            // переустанавливаем callback с новым значением флага
            Methods.ClearEventCallback(_handle);
            Methods.SetEventCallback(_handle, _eventCallback, value);
        }
    }

    /// <summary>
    /// Default directory new torrents are saved to.
    /// A relative save path passed when attaching a torrent is resolved against this.
    /// </summary>
    public string DefaultDownloadPath { get; set; } = Path.Combine(Environment.CurrentDirectory, "downloads");

    /// <summary>
    /// Raised whenever the session produces a notification.
    /// Backed by an unmanaged event loop that starts/stops with the first/last subscriber.
    /// </summary>
    public event EventHandler<SessionNotification>? NotificationRaised;

    /// <summary>
    /// Applies a settings pack to the running session. The change takes effect asynchronously.
    /// </summary>
    public void UpdateSettings(SettingsPack pack)
    {
        ValidateSettingsPack(pack);

        var packHandle = pack.BuildNative();

        try
        {
            Methods.ApplySettingsPack(_handle, packHandle);
        }
        finally
        {
            Methods.FreeSettingsPack(packHandle);
        }
    }

    /// <summary>
    /// Whether the session's DHT node is currently running.
    /// </summary>
    public bool IsDhtRunning
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return Methods.IsSessionDhtRunning(_handle);
        }
    }

    /// <summary>
    /// Adds a bootstrap node to the session's DHT routing table.
    /// </summary>
    /// <param name="host">Hostname or IP address of the node</param>
    /// <param name="port">Port the node listens on</param>
    public void AddDhtNode(string host, int port)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Methods.AddSessionDhtNode(_handle, host, port);
    }

    /// <summary>
    /// Adds one rule to the session's IP filter. Rules accumulate across calls until
    /// <see cref="ClearIpFilter"/> resets the filter.
    /// </summary>
    /// <param name="firstIp">First address in the range (inclusive)</param>
    /// <param name="lastIp">Last address in the range (inclusive)</param>
    /// <param name="blocked">Whether addresses in this range should be blocked</param>
    public void AddIpFilterRule(string firstIp, string lastIp, bool blocked = true)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Methods.AddSessionIpFilterRule(_handle, firstIp, lastIp, blocked);
    }

    /// <summary>
    /// Clears every rule from the session's IP filter.
    /// </summary>
    public void ClearIpFilter()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Methods.ClearSessionIpFilter(_handle);
    }

    /// <summary>
    /// Samples the session's internal counters/gauges and asynchronously waits for the result.
    /// </summary>
    /// <remarks>
    /// Only one sample can be pending at a time - starting a new one while one is already pending
    /// abandons the wait for the first (its own <paramref name="timeout"/> still applies; the
    /// sample itself isn't cancelled).
    /// </remarks>
    /// <param name="cancellationToken">Cancels the wait; the sample itself keeps going regardless</param>
    /// <param name="timeout">Upper bound on the wait, independent of <paramref name="cancellationToken"/>. Defaults to 30s.</param>
    /// <returns>Every sampled metric, keyed by libtorrent's own metric name (e.g. <c>"net.recv_bytes"</c>)</returns>
    /// <exception cref="TimeoutException">No result arrived within <paramref name="timeout"/></exception>
    public async Task<IReadOnlyDictionary<string, long>> GetSessionStatsAsync(CancellationToken cancellationToken, TimeSpan? timeout = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var taskCompletionSource = new TaskCompletionSource<IReadOnlyDictionary<string, long>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingSessionStats = taskCompletionSource;

        Methods.PostSessionStats(_handle);

        return await taskCompletionSource.Task.WaitAsync(timeout ?? TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds a parsed torrent to the session so it can be transferred.
    /// </summary>
    /// <param name="torrent">Parsed torrent to add</param>
    /// <param name="savePath">Where to store/read its contents</param>
    /// <param name="resumeData">
    /// Previously saved resume data (see <see cref="TorrentManager.SaveResumeDataAsync"/>), or
    /// <c>null</c> for a fresh attach. Malformed or stale data is silently ignored rather than
    /// failing the attach.
    /// </param>
    /// <returns>A <see cref="TorrentManager"/> for controlling the transfer</returns>
    /// <exception cref="InvalidOperationException">The session rejected the torrent</exception>
    public TorrentManager AttachTorrent(TorrentInfo torrent, string? savePath = null, byte[]? resumeData = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var infoHash = torrent.Metadata.InfoHash
            ?? throw new InvalidOperationException("Torrent has no v1 info-hash; v2-only torrents are not supported.");

        if (_attachedManagers.ContainsKey(infoHash))
            throw new InvalidOperationException("Torrent is already attached to this session.");

        savePath = ResolveSavePath(savePath);

        var handle = Methods.AttachTorrent(_handle, torrent.InfoHandle, savePath, resumeData, resumeData?.Length ?? 0);
        if (handle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to attach torrent to session.");

        var manager = new TorrentManager(handle, savePath, infoHash, torrent);
        _attachedManagers.TryAdd(infoHash, manager);

        return manager;
    }

    /// <summary>
    /// Adds a magnet link to the session.
    /// </summary>
    /// <param name="magnetUri">Magnet URI to add</param>
    /// <param name="savePath">Where to store the downloaded content</param>
    /// <param name="resumeData">
    /// Previously saved resume data (see <see cref="TorrentManager.SaveResumeDataAsync"/>), or
    /// <c>null</c> for a fresh attach. When it parses successfully it takes over entirely (trackers,
    /// and metadata if it was fetched before the data was saved), taking priority over
    /// <paramref name="magnetUri"/>'s own trackers. Malformed or stale data is silently ignored
    /// rather than failing the attach.
    /// </param>
    /// <returns>A <see cref="TorrentManager"/> for controlling the transfer</returns>
    /// <exception cref="InvalidOperationException">The URI was invalid, or the session rejected it</exception>
    public TorrentManager AttachMagnet(string magnetUri, string? savePath = null, byte[]? resumeData = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        savePath = ResolveSavePath(savePath);

        var handle = Methods.AttachMagnet(_handle, magnetUri, savePath, resumeData, resumeData?.Length ?? 0);
        if (handle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to attach magnet URI to session. Ensure the URI is valid.");

        Span<byte> hashBytes = stackalloc byte[20];
        Methods.GetTorrentHandleInfoHash(handle, hashBytes);

        var infoHash = Convert.ToHexString(hashBytes);

        if (_attachedManagers.ContainsKey(infoHash))
        {
            Methods.DetachTorrent(_handle, handle);
            throw new InvalidOperationException("A torrent with the same info-hash is already attached to this session.");
        }

        var manager = new TorrentManager(handle, savePath, infoHash, null);
        _attachedManagers.TryAdd(infoHash, manager);

        return manager;
    }

    /// <summary>
    /// Removes a torrent from the session and stops its transfer.
    /// </summary>
    /// <param name="manager">Torrent to remove</param>
    public void DetachTorrent(TorrentManager manager)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // manager окончательно удаляется из словаря как только вызов уведомлений (срабатывает,
        // когда торрент полностью удалён на нативной стороне)
        if (!_attachedManagers.ContainsKey(manager.InfoHash))
            throw new InvalidOperationException("Unable to detach torrent from session. Ensure the torrent is attached to this session.");

        if (!manager.TryMarkDetachRequested())
            throw new InvalidOperationException("The torrent is already being detached.");

        manager.Stop();

        // Помечаем отсоединённым СИНХРОННО, до нативного вызова, а не только когда придёт
        // асинхронное уведомление TorrentRemoved - native detach_torrent освобождает torrent_handle
        // сразу же (см. library.cpp), и до этой отметки было окно, где _detached ещё false, а
        // указатель уже висячий: конкурентный вызов из TorrentManager (GetCurrentStatus, GetPeers,
        // ...) или из обработчика алертов (MetadataReceivedHandle) мог уйти в нативный код с уже
        // освобождённой памятью.
        manager.MarkAsDetached();
        Methods.DetachTorrent(_handle, manager.TorrentSessionHandle);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var session in ActiveTorrents)
        {
            try
            {
                DetachTorrent(session);
            }
            catch
            {
                // намеренно игнорируем
            }
        }

        _disposed = true;

        Methods.ClearEventCallback(_handle);
        Methods.FreeSession(_handle);

        GC.SuppressFinalize(this);
    }

    private string ResolveSavePath(string? savePath)
    {
        savePath ??= DefaultDownloadPath;

        if (!Path.IsPathRooted(savePath)) 
            savePath = Path.Combine(DefaultDownloadPath, savePath);

        Directory.CreateDirectory(savePath);

        return Path.GetFullPath(savePath);
    }

    /// <summary>
    /// Ensures a settings pack carries the categories this library needs internally to function correctly.
    /// </summary>
    private static void ValidateSettingsPack(SettingsPack settingsPack)
    {
        // маска уведомлений (alert_mask) должна включать обязательные категории
        var currentMask = settingsPack.Get<AlertMask>()?.Value ?? NotificationCategories.None;
        settingsPack.Set(new AlertMask(currentMask | RequiredNotificationCategories));
    }

    /// <summary>
    /// Converts a raw unmanaged event into its managed equivalent and raises <see cref="NotificationRaised"/>.
    /// The unmanaged library owns the event's memory and frees it once this callback returns.
    /// </summary>
    /// <param name="eventPtr">Pointer to the raw event (see <c>events.h</c>)</param>
    private unsafe void ProxyRaisedEvent(IntPtr eventPtr)
    {
        if (eventPtr == IntPtr.Zero)
            return;

        var forwardNotification = (NotificationType)(*(int*)eventPtr.ToPointer()) switch
        {
            NotificationType.Generic => GenericHandle(eventPtr),
            NotificationType.TorrentStatus => TorrentStatusHandle(eventPtr),
            NotificationType.ClientPerformance => ClientPerformanceHandle(eventPtr),
            NotificationType.Peer => PeerHandle(eventPtr),
            NotificationType.TorrentRemoved => TorrentRemovedHandle(eventPtr),
            NotificationType.MetadataReceived => MetadataReceivedHandle(eventPtr),
            NotificationType.ReadPiece => ReadPieceHandle(eventPtr),
            NotificationType.FileRenamed => FileRenamedHandle(eventPtr),
            NotificationType.StorageMoved => StorageMovedHandle(eventPtr),
            NotificationType.Scrape => ScrapeHandle(eventPtr),
            NotificationType.ResumeData => ResumeDataHandle(eventPtr),
            NotificationType.SessionStats => SessionStatsHandle(eventPtr),
            _ => null
        };

        if (forwardNotification is not null) 
            NotificationRaised?.Invoke(this, forwardNotification);
    }

    private static SessionNotification GenericHandle(IntPtr eventPtr)
    {
        var genericNotification = Marshal.PtrToStructure<EventBase>(eventPtr);
        return new SessionNotification(genericNotification);
    }

    private SessionNotification? TorrentStatusHandle(IntPtr eventPtr)
    {
        var statusNotification = Marshal.PtrToStructure<TorrentStatusEvent>(eventPtr);
        
        return _attachedManagers.TryGetValue(Convert.ToHexString(statusNotification.InfoHash), out var torrentManager)
            ? new TorrentStatusNotification(statusNotification, torrentManager) :
            null;
    }

    private static SessionNotification ClientPerformanceHandle(IntPtr eventPtr)
    {
        var performanceNotification = Marshal.PtrToStructure<PerformanceWarningEvent>(eventPtr);
        return new PerformanceWarningNotification(performanceNotification);
    }

    private SessionNotification? PeerHandle(IntPtr eventPtr)
    {
        var peerNotification = Marshal.PtrToStructure<PeerEvent>(eventPtr);
        return _attachedManagers.TryGetValue(Convert.ToHexString(peerNotification.InfoHash), out var peerSubject) 
            ? new PeerNotification(peerNotification, peerSubject) 
            : null;
    }

    private SessionNotification? TorrentRemovedHandle(IntPtr eventPtr)
    {
        var removedNotification = Marshal.PtrToStructure<TorrentRemovedEvent>(eventPtr);
        
        if (!_attachedManagers.TryRemove(Convert.ToHexString(removedNotification.InfoHash), out var manager))
            return null;
        
        // помечаем как отсоединенный, чтобы предотвратить дальнейшее использование
        manager.MarkAsDetached();
        return new TorrentRemovedNotification(removedNotification, manager);

    }

    private SessionNotification? MetadataReceivedHandle(IntPtr eventPtr)
    {
        var metaNotification = Marshal.PtrToStructure<MetadataReceivedEvent>(eventPtr);
        if (!_attachedManagers.TryGetValue(Convert.ToHexString(metaNotification.InfoHash), out var metaSubject))
            return null;

        // торрент уже отсоединяется (или отсоединён) - его native handle может быть уже освобождён,
        // дальше его трогать нельзя (см. IsDetached / DetachTorrent).
        if (metaSubject.IsDetached)
            return new MetadataReceivedNotification(metaNotification, metaSubject);

        var infoHandle = Methods.GetHandleTorrentInfo(metaSubject.TorrentSessionHandle);
        if (infoHandle == IntPtr.Zero) 
            return new MetadataReceivedNotification(metaNotification, metaSubject);
        
        if (metaSubject.PauseAfterMetadata) 
            Methods.StopTorrent(metaSubject.TorrentSessionHandle);

        metaSubject.OnMetadataReceived(new TorrentInfo(infoHandle));

        return new MetadataReceivedNotification(metaNotification, metaSubject);
    }

    private SessionNotification? ReadPieceHandle(IntPtr eventPtr)
    {
        var readPiceEvent = Marshal.PtrToStructure<ReadPieceEvent>(eventPtr);
        if (!_attachedManagers.TryGetValue(Convert.ToHexString(readPiceEvent.InfoHash), out var pieceSubject))
            return null;

        var buffer = Array.Empty<byte>();
        if (readPiceEvent is { Succeeded: true, Size: > 0 })
        {
            buffer = new byte[readPiceEvent.Size];
            Marshal.Copy(readPiceEvent.Buffer, buffer, 0, readPiceEvent.Size);
        }

        // резолвит ожидающий вызов ReadPieceAsync, если он есть, до того как уведомление уйдёт наружу
        pieceSubject.OnPieceRead(readPiceEvent.Piece, buffer, readPiceEvent.Succeeded);

        return new ReadPieceNotification(readPiceEvent, pieceSubject, buffer);
    }

    private SessionNotification? FileRenamedHandle(IntPtr eventPtr)
    {
        var renamedEvent = Marshal.PtrToStructure<FileRenamedEvent>(eventPtr);
        if (!_attachedManagers.TryGetValue(Convert.ToHexString(renamedEvent.InfoHash), out var subject))
            return null;

        // резолвит ожидающий вызов RenameFileAsync, если он есть, до того как уведомление уйдёт наружу
        subject.OnFileRenamed(renamedEvent.FileIndex, renamedEvent.Succeeded);

        return new FileRenamedNotification(renamedEvent, subject);
    }

    private SessionNotification? StorageMovedHandle(IntPtr eventPtr)
    {
        var movedEvent = Marshal.PtrToStructure<StorageMovedEvent>(eventPtr);
        if (!_attachedManagers.TryGetValue(Convert.ToHexString(movedEvent.InfoHash), out var subject))
            return null;

        // резолвит ожидающий вызов MoveStorageAsync, если он есть, до того как уведомление уйдёт наружу
        subject.OnStorageMoved(movedEvent.Succeeded);

        return new StorageMovedNotification(movedEvent, subject);
    }

    private SessionNotification? ScrapeHandle(IntPtr eventPtr)
    {
        var scrapeEvent = Marshal.PtrToStructure<ScrapeEvent>(eventPtr);
        if (!_attachedManagers.TryGetValue(Convert.ToHexString(scrapeEvent.InfoHash), out var subject))
            return null;

        // резолвит ожидающий вызов ScrapeTrackerAsync, если он есть, до того как уведомление уйдёт наружу
        subject.OnScrapeCompleted(scrapeEvent.Succeeded, scrapeEvent.Incomplete, scrapeEvent.Complete);

        return new ScrapeNotification(scrapeEvent, subject);
    }

    private SessionNotification? ResumeDataHandle(IntPtr eventPtr)
    {
        var resumeEvent = Marshal.PtrToStructure<ResumeDataEvent>(eventPtr);
        if (!_attachedManagers.TryGetValue(Convert.ToHexString(resumeEvent.InfoHash), out var subject))
            return null;

        var data = Array.Empty<byte>();
        if (resumeEvent is { Succeeded: true, Size: > 0 })
        {
            data = new byte[resumeEvent.Size];
            Marshal.Copy(resumeEvent.Buffer, data, 0, resumeEvent.Size);
        }

        // резолвит ожидающий вызов SaveResumeDataAsync, если он есть, до того как уведомление уйдёт наружу
        subject.OnResumeDataSaved(resumeEvent.Succeeded, data);

        return new ResumeDataNotification(resumeEvent, subject, data);
    }

    private SessionNotification SessionStatsHandle(IntPtr eventPtr)
    {
        var statsEvent = Marshal.PtrToStructure<SessionStatsEvent>(eventPtr);

        var values = Array.Empty<long>();
        if (statsEvent.Count > 0)
        {
            values = new long[statsEvent.Count];
            Marshal.Copy(statsEvent.Values, values, 0, statsEvent.Count);
        }

        var metrics = BuildSessionStatsMetrics(values);

        // резолвит ожидающий вызов GetSessionStatsAsync, если он есть, до того как уведомление уйдёт наружу
        _pendingSessionStats?.TrySetResult(metrics);

        return new SessionStatsNotification(statsEvent, metrics);
    }

    private static IReadOnlyDictionary<string, long> BuildSessionStatsMetrics(long[] values)
    {
        var table = GetSessionStatsMetricTable();
        var result = new Dictionary<string, long>(table.Count);

        foreach (var (name, valueIndex) in table)
        {
            if (valueIndex >= 0 && valueIndex < values.Length)
                result[name] = values[valueIndex];
        }

        return result;
    }

    // таблица не зависит от конкретной сессии, поэтому получаем и кэшируем один раз на процесс.
    private static IReadOnlyList<(string Name, int ValueIndex)> GetSessionStatsMetricTable()
    {
        if (_sessionStatsMetricTable is not null)
            return _sessionStatsMetricTable;

        Methods.GetSessionStatsMetrics(out var list);
        try
        {
            var result = new List<(string, int)>(list.Length);
            var size = Marshal.SizeOf<SessionStatsMetric>();

            for (var i = 0; i < list.Length; i++)
            {
                var entry = Marshal.PtrToStructure<SessionStatsMetric>(list.Metrics + i * size);
                result.Add((entry.Name, entry.ValueIndex));
            }

            return _sessionStatsMetricTable = result;
        }
        finally
        {
            Methods.FreeSessionStatsMetricList(ref list);
        }
    }
}
