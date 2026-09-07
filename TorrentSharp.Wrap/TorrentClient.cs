using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using TorrentSharp.Wrap.Configurations;
using TorrentSharp.Wrap.Notifications;
using TorrentSharp.Wrap.Enums;
using TorrentSharp.Wrap.Imports.Events;
using Methods = TorrentSharp.Wrap.Imports.Methods;

namespace TorrentSharp.Wrap;

/// <summary>
/// A client that manages a libtorrent session and its attached torrents.
/// </summary>
public class TorrentClient : IDisposable
{
    // Storage нужна для read_piece_alert (ReadPiece) - без неё libtorrent никогда не выдаст
    // этот event, независимо от того, как выставлены дедлайны.
    private const NotificationCategories RequiredNotificationCategories = NotificationCategories.Status | NotificationCategories.Storage;

    private readonly ConcurrentDictionary<string, TorrentManager> _attachedManagers = new(StringComparer.OrdinalIgnoreCase);

    // ссылку на делегате нужно держать явно, иначе GC может собрать её раньше времени
    private readonly Methods.SessionEventCallback _eventCallback;
    private readonly IntPtr _handle;

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
    /// Adds a parsed torrent to the session so it can be transferred.
    /// </summary>
    /// <param name="torrent">Parsed torrent to add</param>
    /// <param name="savePath">Where to store/read its contents</param>
    /// <returns>A <see cref="TorrentManager"/> for controlling the transfer</returns>
    /// <exception cref="InvalidOperationException">The session rejected the torrent</exception>
    public TorrentManager AttachTorrent(TorrentInfo torrent, string? savePath = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var infoHash = torrent.Metadata.InfoHash
            ?? throw new InvalidOperationException("Torrent has no v1 info-hash; v2-only torrents are not supported.");

        if (_attachedManagers.ContainsKey(infoHash))
            throw new InvalidOperationException("Torrent is already attached to this session.");

        savePath = ResolveSavePath(savePath);

        var handle = Methods.AttachTorrent(_handle, torrent.InfoHandle, savePath);
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
    /// <returns>A <see cref="TorrentManager"/> for controlling the transfer</returns>
    /// <exception cref="InvalidOperationException">The URI was invalid, or the session rejected it</exception>
    public TorrentManager AttachMagnet(string magnetUri, string? savePath = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        savePath = ResolveSavePath(savePath);

        var handle = Methods.AttachMagnet(_handle, magnetUri, savePath);
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
        settingsPack.Set("alert_mask", settingsPack.Get<int>("alert_mask") | (int)RequiredNotificationCategories);
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
}
