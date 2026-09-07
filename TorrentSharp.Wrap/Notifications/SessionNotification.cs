using TorrentSharp.Wrap.Enums;
using TorrentSharp.Wrap.Imports.Events;

namespace TorrentSharp.Wrap.Notifications;

public class SessionNotification(EventBase @event) : EventArgs
{
    public NotificationType Type { get; } = @event.Type;

    public int Category { get; } = @event.Category;

    public DateTimeOffset Timestamp { get; } = DateTimeOffset.FromUnixTimeSeconds(@event.Timestamp);

    public string Message { get; } = @event.Message;
}
