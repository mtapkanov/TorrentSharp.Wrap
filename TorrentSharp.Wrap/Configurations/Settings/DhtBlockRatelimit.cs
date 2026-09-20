namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the max number of packets per second a DHT node is allowed to send without getting banned.
/// </summary>
public sealed record DhtBlockRatelimit(int Value) : ISettingsEntry<DhtBlockRatelimit>
{
    public static string Key => "dht_block_ratelimit";
    public static DhtBlockRatelimit FromValue(object value) => new((int)value);
    object ISettingsEntry<DhtBlockRatelimit>.Value => Value;
}
