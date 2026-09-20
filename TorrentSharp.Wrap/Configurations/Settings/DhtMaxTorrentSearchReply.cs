namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the max number of torrents to return in a torrent search query to the DHT
/// </summary>
public sealed record DhtMaxTorrentSearchReply(int Value) : ISettingsEntry<DhtMaxTorrentSearchReply>
{
    public static string Key => "dht_max_torrent_search_reply";
    public static DhtMaxTorrentSearchReply FromValue(object value) => new((int)value);
    object ISettingsEntry<DhtMaxTorrentSearchReply>.Value => Value;
}
