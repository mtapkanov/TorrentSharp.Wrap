using TorrentSharp.Wrap.Imports.Events;

namespace TorrentSharp.Wrap.Notifications;

/// <summary>
/// Raised when the session fails to bind a listen socket on some interface/port/protocol -
/// session-wide, not tied to any torrent. If this fires for the socket libtorrent picked for DHT,
/// DHT will report itself as running while never actually bootstrapping (no known nodes) - see
/// <see cref="ListenSucceededNotification"/> for the positive counterpart.
/// </summary>
public class ListenFailedNotification(ListenFailedEvent @event) : SessionNotification(@event.Info)
{
    /// <summary>The underlying OS error code (an errno-equivalent on POSIX, e.g. EADDRINUSE).</summary>
    public int ErrorValue { get; } = @event.ErrorValue;

    /// <summary>libtorrent's raw operation_t for the operation that failed (see libtorrent/operations.hpp).</summary>
    public byte Operation { get; } = @event.Operation;

    /// <summary>libtorrent's raw socket_type_t (see libtorrent/socket_type.hpp - tcp = 0, utp = 3, ...).</summary>
    public byte SocketType { get; } = @event.SocketType;

    /// <summary>The port libtorrent attempted to listen on.</summary>
    public int Port { get; } = @event.Port;

    /// <summary>The network device or address libtorrent attempted to listen on.</summary>
    public string ListenInterface { get; } = @event.ListenInterface;
}
