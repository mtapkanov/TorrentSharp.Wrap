namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// when this is true, nodes whose IDs are derived from their source IP according to BEP 42_ are preferred in the
/// routing table.
/// </summary>
public sealed record DhtPreferVerifiedNodeIds(bool Value) : ISettingsEntry<DhtPreferVerifiedNodeIds>
{
    public static string Key => "dht_prefer_verified_node_ids";
    public static DhtPreferVerifiedNodeIds FromValue(object value) => new((bool)value);
    object ISettingsEntry<DhtPreferVerifiedNodeIds>.Value => Value;
}
