namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>local_service_announce_interval</c> is the time between local network announces for a torrent. This interval
/// is specified in seconds.
/// </summary>
public sealed record LocalServiceAnnounceInterval(int Value) : ISettingsEntry<LocalServiceAnnounceInterval>
{
    public static string Key => "local_service_announce_interval";
    public static LocalServiceAnnounceInterval FromValue(object value) => new((int)value);
    object ISettingsEntry<LocalServiceAnnounceInterval>.Value => Value;
}
