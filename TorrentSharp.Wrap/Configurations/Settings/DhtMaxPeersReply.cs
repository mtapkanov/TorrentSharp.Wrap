namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the maximum number of peers to send in a reply to <c>get_peers</c>
/// </summary>
public sealed record DhtMaxPeersReply(int Value) : ISettingsEntry<DhtMaxPeersReply>
{
    public static string Key => "dht_max_peers_reply";
    public static DhtMaxPeersReply FromValue(object value) => new((int)value);
    object ISettingsEntry<DhtMaxPeersReply>.Value => Value;
}
