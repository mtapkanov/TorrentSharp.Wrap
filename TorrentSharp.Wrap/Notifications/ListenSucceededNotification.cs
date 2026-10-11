using TorrentSharp.Wrap.Imports.Events;

namespace TorrentSharp.Wrap.Notifications;

/// <summary>The positive counterpart to <see cref="ListenFailedNotification"/> - the listen socket bound successfully.</summary>
public class ListenSucceededNotification(ListenSucceededEvent @event) : SessionNotification(@event.Info)
{
    /// <summary>libtorrent's raw socket_type_t (see libtorrent/socket_type.hpp - tcp = 0, utp = 3, ...).</summary>
    public byte SocketType { get; } = @event.SocketType;

    /// <summary>The port libtorrent ended up listening on.</summary>
    public int Port { get; } = @event.Port;

    /// <summary>The local interface address libtorrent ended up listening on.</summary>
    public string Address { get; } = @event.Address;
}
