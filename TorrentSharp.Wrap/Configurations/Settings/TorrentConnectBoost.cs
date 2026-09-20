namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>torrent_connect_boost</c> is the number of peers to try to connect to immediately when the first tracker
/// response is received for a torrent. This is a boost to given to new torrents to accelerate them starting up. The
/// normal connect scheduler is run once every second, this allows peers to be connected immediately instead of
/// waiting for the session tick to trigger connections. This may not be set higher than 255.
/// </summary>
public sealed record TorrentConnectBoost(int Value) : ISettingsEntry<TorrentConnectBoost>
{
    public static string Key => "torrent_connect_boost";
    public static TorrentConnectBoost FromValue(object value) => new((int)value);
    object ISettingsEntry<TorrentConnectBoost>.Value => Value;
}
