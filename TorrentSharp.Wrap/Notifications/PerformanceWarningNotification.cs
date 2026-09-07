using TorrentSharp.Wrap.Enums;
using TorrentSharp.Wrap.Imports.Events;

namespace TorrentSharp.Wrap.Notifications;

public class PerformanceWarningNotification(PerformanceWarningEvent @event) : SessionNotification(@event.Info)
{
    public PerformanceWarningType WarningCode { get; } = @event.WarningCode;
}
