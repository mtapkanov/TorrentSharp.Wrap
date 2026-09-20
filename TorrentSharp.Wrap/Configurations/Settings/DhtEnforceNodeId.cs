namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// when set, node's whose IDs that are not correctly generated based on its external IP are ignored. When a query
/// arrives from such node, an error message is returned with a message saying "invalid node ID".
/// </summary>
public sealed record DhtEnforceNodeId(bool Value) : ISettingsEntry<DhtEnforceNodeId>
{
    public static string Key => "dht_enforce_node_id";
    public static DhtEnforceNodeId FromValue(object value) => new((bool)value);
    object ISettingsEntry<DhtEnforceNodeId>.Value => Value;
}
