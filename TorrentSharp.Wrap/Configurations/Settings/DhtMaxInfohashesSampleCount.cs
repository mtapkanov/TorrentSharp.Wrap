namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the maximum number of elements in the sampled subset of info-hashes. If this number is too big, expect the DHT
/// storage implementations to clamp it in order to allow UDP packets go through
/// </summary>
public sealed record DhtMaxInfohashesSampleCount(int Value) : ISettingsEntry<DhtMaxInfohashesSampleCount>
{
    public static string Key => "dht_max_infohashes_sample_count";
    public static DhtMaxInfohashesSampleCount FromValue(object value) => new((int)value);
    object ISettingsEntry<DhtMaxInfohashesSampleCount>.Value => Value;
}
