namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>announce_port</c> is the port passed along as the <c>port</c> parameter to remote trackers such as HTTP or
/// DHT. This setting does not affect the effective listening port nor local service discovery announcements. If
/// left as zero (default), the listening port value is used.
/// </summary>
public sealed record AnnouncePort(int Value) : ISettingsEntry<AnnouncePort>
{
    public static string Key => "announce_port";
    public static AnnouncePort FromValue(object value) => new((int)value);
    object ISettingsEntry<AnnouncePort>.Value => Value;
}
