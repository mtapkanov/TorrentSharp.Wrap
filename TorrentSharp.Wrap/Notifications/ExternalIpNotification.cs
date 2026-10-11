using TorrentSharp.Wrap.Imports.Events;

namespace TorrentSharp.Wrap.Notifications;

/// <summary>
/// Raised whenever libtorrent learns or changes its believed external IP address (from a tracker
/// that supports it, or from peers via the extension protocol) - session-wide, not tied to any torrent.
/// </summary>
public class ExternalIpNotification(ExternalIpEvent @event) : SessionNotification(@event.Info)
{
    public string ExternalAddress { get; } = @event.ExternalAddress;
}
