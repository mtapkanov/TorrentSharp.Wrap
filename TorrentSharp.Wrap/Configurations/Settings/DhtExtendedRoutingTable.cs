namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// makes the first buckets in the DHT routing table fit 128, 64, 32 and 16 nodes respectively, as opposed to the
/// standard size of 8. All other buckets have size 8 still.
/// </summary>
public sealed record DhtExtendedRoutingTable(bool Value) : ISettingsEntry<DhtExtendedRoutingTable>
{
    public static string Key => "dht_extended_routing_table";
    public static DhtExtendedRoutingTable FromValue(object value) => new((bool)value);
    object ISettingsEntry<DhtExtendedRoutingTable>.Value => Value;
}
