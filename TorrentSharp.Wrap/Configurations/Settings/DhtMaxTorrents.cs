namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the total number of torrents to track from the DHT. This is simply an upper limit to make sure malicious DHT
/// nodes cannot make us allocate an unbounded amount of memory.
/// </summary>
public sealed record DhtMaxTorrents(int Value) : ISettingsEntry<DhtMaxTorrents>
{
    public static string Key => "dht_max_torrents";
    public static DhtMaxTorrents FromValue(object value) => new((int)value);
    object ISettingsEntry<DhtMaxTorrents>.Value => Value;
}
