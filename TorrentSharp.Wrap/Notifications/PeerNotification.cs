using System.Net;
using TorrentSharp.Wrap.Enums;
using TorrentSharp.Wrap.Imports.Events;

namespace TorrentSharp.Wrap.Notifications;

public class PeerNotification(PeerEvent @event, TorrentManager torrentManager) : SessionNotification(@event.Info)
{
    public TorrentManager TorrentManager { get; } = torrentManager;
    public PeerNotificationType NotificationType { get; } = @event.NotificationType;
    public IPAddress Address { get; } = new IPAddress(@event.V6Address);
}
