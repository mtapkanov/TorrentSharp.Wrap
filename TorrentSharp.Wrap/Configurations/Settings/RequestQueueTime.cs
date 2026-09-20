namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the length of the request queue given in the number of seconds it should take for the other end to send all the
/// pieces. i.e. the actual number of requests depends on the download rate and this number.
/// </summary>
public sealed record RequestQueueTime(int Value) : ISettingsEntry<RequestQueueTime>
{
    public static string Key => "request_queue_time";
    public static RequestQueueTime FromValue(object value) => new((int)value);
    object ISettingsEntry<RequestQueueTime>.Value => Value;
}
