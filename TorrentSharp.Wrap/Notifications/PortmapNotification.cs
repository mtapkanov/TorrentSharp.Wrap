using TorrentSharp.Wrap.Enums;
using TorrentSharp.Wrap.Imports.Events;

namespace TorrentSharp.Wrap.Notifications;

/// <summary>
/// Raised when a NAT-PMP/UPnP router successfully mapped one of our ports - the standard proxy most
/// torrent clients use for "is my port open" without an external reachability check.
/// </summary>
public class PortmapNotification(PortmapEvent @event) : SessionNotification(@event.Info)
{
    /// <summary>The external port the router agreed to forward.</summary>
    public int ExternalPort { get; } = @event.ExternalPort;

    public PortmapTransport Transport { get; } = (PortmapTransport)@event.Transport;
}
