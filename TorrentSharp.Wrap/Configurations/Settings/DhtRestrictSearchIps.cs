namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// determines if DHT searches should prevent adding nodes with IPs with very close CIDR distance. This also
/// defaults to true and helps mitigate certain attacks on the DHT.
/// </summary>
public sealed record DhtRestrictSearchIps(bool Value) : ISettingsEntry<DhtRestrictSearchIps>
{
    public static string Key => "dht_restrict_search_ips";
    public static DhtRestrictSearchIps FromValue(object value) => new((bool)value);
    object ISettingsEntry<DhtRestrictSearchIps>.Value => Value;
}
