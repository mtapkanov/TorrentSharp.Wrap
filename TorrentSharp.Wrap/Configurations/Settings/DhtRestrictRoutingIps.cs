namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// determines if the routing table entries should restrict entries to one per IP. This defaults to true, which
/// helps mitigate some attacks on the DHT. It prevents adding multiple nodes with IPs with a very close CIDR
/// distance.
/// </summary>
public sealed record DhtRestrictRoutingIps(bool Value) : ISettingsEntry<DhtRestrictRoutingIps>
{
    public static string Key => "dht_restrict_routing_ips";
    public static DhtRestrictRoutingIps FromValue(object value) => new((bool)value);
    object ISettingsEntry<DhtRestrictRoutingIps>.Value => Value;
}
