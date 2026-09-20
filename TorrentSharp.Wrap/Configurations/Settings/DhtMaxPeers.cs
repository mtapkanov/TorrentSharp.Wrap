namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the max number of peers to store per torrent (for the DHT)
/// </summary>
public sealed record DhtMaxPeers(int Value) : ISettingsEntry<DhtMaxPeers>
{
    public static string Key => "dht_max_peers";
    public static DhtMaxPeers FromValue(object value) => new((int)value);
    object ISettingsEntry<DhtMaxPeers>.Value => Value;
}
