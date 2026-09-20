namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// if true, peer connections are made (and accepted) over the configured proxy, if any. Web seeds as well as
/// regular bittorrent peer connections are considered "peer connections". Anything transporting actual torrent
/// payload (trackers and DHT traffic are not considered peer connections).
/// </summary>
public sealed record ProxyPeerConnections(bool Value) : ISettingsEntry<ProxyPeerConnections>
{
    public static string Key => "proxy_peer_connections";
    public static ProxyPeerConnections FromValue(object value) => new((bool)value);
    object ISettingsEntry<ProxyPeerConnections>.Value => Value;
}
