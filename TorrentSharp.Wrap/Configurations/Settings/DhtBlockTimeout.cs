namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the number of seconds a DHT node is banned if it exceeds the rate limit. The rate limit is averaged over 10
/// seconds to allow for bursts above the limit.
/// </summary>
public sealed record DhtBlockTimeout(int Value) : ISettingsEntry<DhtBlockTimeout>
{
    public static string Key => "dht_block_timeout";
    public static DhtBlockTimeout FromValue(object value) => new((int)value);
    object ISettingsEntry<DhtBlockTimeout>.Value => Value;
}
