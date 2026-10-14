using TorrentSharp.Wrap.Configurations;
using TorrentSharp.Wrap.Enums;
using TorrentSharp.Wrap.Notifications;

namespace TorrentSharp.Wrap;

/// <summary>
/// A libtorrent session and its attached torrents. Configured once, as it is created (see
/// <see cref="TorrentClient(Action{ITorrentClientStartupOptions})"/>), and changed while it runs through
/// <see cref="ChangeOptions"/> only.
/// </summary>
public interface ITorrentClient : IDisposable
{
    /// <summary>
    /// Torrents currently attached to this session.
    /// </summary>
    IEnumerable<TorrentManager> ActiveTorrents { get; }

    /// <summary>
    /// Raised for every notification the session produces, on the thread that dispatches them - never block
    /// a handler on anything that is itself answered by a notification. A notification nobody is subscribed
    /// to when it arrives is dropped; subscribe through
    /// <see cref="ITorrentClientStartupOptions.OnNotification"/> to see those raised while the session starts.
    /// </summary>
    event EventHandler<SessionNotification>? NotificationRaised;

    /// <summary>
    /// Whether the session's DHT node is currently running.
    /// </summary>
    bool IsDhtRunning { get; }

    /// <summary>
    /// Adds a node to the session's DHT routing table.
    /// </summary>
    /// <param name="host">Hostname or IP address of the node</param>
    /// <param name="port">Port the node listens on</param>
    void AddDhtNode(string host, int port);

    /// <summary>
    /// Samples the session's internal counters/gauges and asynchronously waits for the result.
    /// </summary>
    /// <remarks>
    /// Only one sample can be pending at a time - starting a new one while one is already pending
    /// abandons the wait for the first (its own <paramref name="timeout"/> still applies; the
    /// sample itself isn't cancelled).
    /// </remarks>
    /// <param name="cancellationToken">Cancels the wait; the sample itself keeps going regardless</param>
    /// <param name="timeout">Upper bound on the wait, independent of <paramref name="cancellationToken"/>. Defaults to the configured default timeout.</param>
    /// <returns>Every sampled metric, keyed by libtorrent's own metric name (e.g. <c>"net.recv_bytes"</c>)</returns>
    /// <exception cref="TimeoutException">No result arrived within <paramref name="timeout"/></exception>
    Task<IReadOnlyDictionary<string, long>> GetSessionStatsAsync(CancellationToken cancellationToken, TimeSpan? timeout = null);

    /// <summary>
    /// Adds a parsed torrent to the session, paused - call <see cref="TorrentManager.Start"/> to begin.
    /// </summary>
    /// <param name="torrent">Parsed torrent to add</param>
    /// <param name="savePath">Where to store/read its contents</param>
    /// <param name="resumeData">
    /// Previously saved resume data (see <see cref="TorrentManager.SaveResumeDataAsync(CancellationToken, TimeSpan?)"/>),
    /// or <c>null</c> for a fresh attach. Malformed or stale data, or data for another torrent, is ignored
    /// rather than failing the attach.
    /// </param>
    /// <param name="downloadByDefault">
    /// When <c>false</c>, every file starts at <see cref="FileDownloadPriority.DoNotDownload"/>
    /// instead of the usual default - useful to inspect a torrent (or let the caller pick files)
    /// before committing to downloading anything.
    /// </param>
    /// <returns>A <see cref="TorrentManager"/> for controlling the transfer</returns>
    /// <exception cref="InvalidOperationException">The session rejected the torrent</exception>
    TorrentManager AttachTorrent(TorrentInfo torrent, string? savePath = null, byte[]? resumeData = null, bool downloadByDefault = true);

    /// <summary>
    /// Adds a magnet link to the session, paused - call <see cref="TorrentManager.Start"/> to begin.
    /// </summary>
    /// <param name="magnetUri">Magnet URI to add</param>
    /// <param name="savePath">Where to store the downloaded content</param>
    /// <param name="resumeData">
    /// Previously saved resume data (see <see cref="TorrentManager.SaveResumeDataAsync(CancellationToken, TimeSpan?)"/>),
    /// or <c>null</c> for a fresh attach. When it parses and is for this torrent it takes over entirely
    /// (trackers, and metadata if it was saved with it). Malformed or stale data, or data for another
    /// torrent, is ignored rather than failing the attach.
    /// </param>
    /// <param name="downloadByDefault">
    /// When <c>false</c>, every file discovered once metadata arrives starts at
    /// <see cref="FileDownloadPriority.DoNotDownload"/> instead of the usual default - useful to
    /// inspect a magnet (or let the caller pick files) before committing to downloading anything,
    /// without racing the moment metadata is received.
    /// </param>
    /// <returns>A <see cref="TorrentManager"/> for controlling the transfer</returns>
    /// <exception cref="InvalidOperationException">The URI was invalid, or the session rejected it</exception>
    TorrentManager AttachMagnet(string magnetUri, string? savePath = null, byte[]? resumeData = null, bool downloadByDefault = true);

    /// <summary>
    /// Removes a torrent from the session and stops its transfer.
    /// </summary>
    /// <param name="manager">Torrent to remove</param>
    void DetachTorrent(TorrentManager manager);

    /// <summary>
    /// Changes the running session's configuration. Only what <paramref name="change"/> sets is changed;
    /// everything else stays as it is. libtorrent applies the settings asynchronously.
    /// </summary>
    /// <example>
    /// <code>
    /// client.ChangeOptions(options => options
    ///     .Network(net => net.ListenOn(IPAddress.Any, 51413))
    ///     .Limits(limits => limits.Download(0).Upload(512 * 1024)));
    /// </code>
    /// </example>
    void ChangeOptions(Action<ITorrentClientOptions> change);

    /// <summary>
    /// The session's DHT state - its node ids and the nodes it knows - for
    /// <see cref="ITorrentClientStartupOptions.State"/> on the next start.
    /// </summary>
    /// <exception cref="InvalidOperationException">libtorrent couldn't produce it</exception>
    byte[] GetState();
}
