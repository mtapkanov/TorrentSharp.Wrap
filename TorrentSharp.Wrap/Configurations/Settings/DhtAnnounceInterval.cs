namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>dht_announce_interval</c> is the number of seconds between announcing torrents to the distributed hash table
/// (DHT).
/// </summary>
public sealed record DhtAnnounceInterval(int Value) : ISettingsEntry<DhtAnnounceInterval>
{
    public static string Key => "dht_announce_interval";
    public static DhtAnnounceInterval FromValue(object value) => new((int)value);
    object ISettingsEntry<DhtAnnounceInterval>.Value => Value;
}
