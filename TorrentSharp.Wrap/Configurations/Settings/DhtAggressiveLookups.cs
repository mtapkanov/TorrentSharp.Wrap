namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// slightly changes the lookup behavior in terms of how many outstanding requests we keep. Instead of having branch
/// factor be a hard limit, we always keep *branch factor* outstanding requests to the closest nodes. i.e. every
/// time we get results back with closer nodes, we query them right away. It lowers the lookup times at the cost of
/// more outstanding queries.
/// </summary>
public sealed record DhtAggressiveLookups(bool Value) : ISettingsEntry<DhtAggressiveLookups>
{
    public static string Key => "dht_aggressive_lookups";
    public static DhtAggressiveLookups FromValue(object value) => new((bool)value);
    object ISettingsEntry<DhtAggressiveLookups>.Value => Value;
}
