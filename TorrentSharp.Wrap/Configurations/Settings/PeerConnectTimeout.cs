namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>peer_connect_timeout</c> the number of seconds to wait after a connection attempt is initiated to a peer
/// until it is considered as having timed out. This setting is especially important in case the number of half-open
/// connections are limited, since stale half-open connection may delay the connection of other peers considerably.
/// </summary>
public sealed record PeerConnectTimeout(int Value) : ISettingsEntry<PeerConnectTimeout>
{
    public static string Key => "peer_connect_timeout";
    public static PeerConnectTimeout FromValue(object value) => new((int)value);
    object ISettingsEntry<PeerConnectTimeout>.Value => Value;
}
