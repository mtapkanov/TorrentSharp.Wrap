namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// limits the number of concurrent HTTP tracker announces. Once the limit is hit, tracker requests are queued and
/// issued when an outstanding announce completes.
/// </summary>
public sealed record MaxConcurrentHttpAnnounces(int Value) : ISettingsEntry<MaxConcurrentHttpAnnounces>
{
    public static string Key => "max_concurrent_http_announces";
    public static MaxConcurrentHttpAnnounces FromValue(object value) => new((int)value);
    object ISettingsEntry<MaxConcurrentHttpAnnounces>.Value => Value;
}
