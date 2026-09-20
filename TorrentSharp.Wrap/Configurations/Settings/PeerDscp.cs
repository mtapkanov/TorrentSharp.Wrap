namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>peer_dscp</c> determines the DSCP field in the IP header of every packet sent to peers (including web seeds).
/// <c>0x0</c> means no marking, <c>0x04</c> represents Lower Effort. For more details see RFC 8622_.
/// </summary>
public sealed record PeerDscp(int Value) : ISettingsEntry<PeerDscp>
{
    public static string Key => "peer_dscp";
    public static PeerDscp FromValue(object value) => new((int)value);
    object ISettingsEntry<PeerDscp>.Value => Value;
}
