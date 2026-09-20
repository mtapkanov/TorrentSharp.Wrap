namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>alert_queue_size</c> is the maximum number of alerts queued up internally. If alerts are not popped, the
/// queue will eventually fill up to this level. Once the alert queue is full, additional alerts will be dropped,
/// and not delivered to the client. Once the client drains the queue, new alerts may be delivered again. In order
/// to know that alerts have been dropped, see session_handle::dropped_alerts().
/// </summary>
public sealed record AlertQueueSize(int Value) : ISettingsEntry<AlertQueueSize>
{
    public static string Key => "alert_queue_size";
    public static AlertQueueSize FromValue(object value) => new((int)value);
    object ISettingsEntry<AlertQueueSize>.Value => Value;
}
