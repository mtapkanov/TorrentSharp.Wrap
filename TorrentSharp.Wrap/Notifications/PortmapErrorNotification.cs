using TorrentSharp.Wrap.Enums;
using TorrentSharp.Wrap.Imports.Events;

namespace TorrentSharp.Wrap.Notifications;

/// <summary>Raised when a NAT-PMP/UPnP router was found but mapping a port on it failed.</summary>
public class PortmapErrorNotification(PortmapErrorEvent @event) : SessionNotification(@event.Info)
{
    /// <summary>The underlying OS error code.</summary>
    public int ErrorValue { get; } = @event.ErrorValue;

    public PortmapTransport Transport { get; } = (PortmapTransport)@event.Transport;
}
