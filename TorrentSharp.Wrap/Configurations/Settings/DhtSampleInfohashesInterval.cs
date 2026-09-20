namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the info-hashes sample recomputation interval (in seconds). The node will precompute a subset of the tracked
/// info-hashes and return that instead of calculating it upon each request. The permissible range is between 0 and
/// 21600 seconds (inclusive).
/// </summary>
public sealed record DhtSampleInfohashesInterval(int Value) : ISettingsEntry<DhtSampleInfohashesInterval>
{
    public static string Key => "dht_sample_infohashes_interval";
    public static DhtSampleInfohashesInterval FromValue(object value) => new((int)value);
    object ISettingsEntry<DhtSampleInfohashesInterval>.Value => Value;
}
