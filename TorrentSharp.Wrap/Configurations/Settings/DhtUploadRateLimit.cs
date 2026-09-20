namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the number of bytes per second (on average) the DHT is allowed to send. If the incoming requests causes to many
/// bytes to be sent in responses, incoming requests will be dropped until the quota has been replenished.
/// </summary>
public sealed record DhtUploadRateLimit(int Value) : ISettingsEntry<DhtUploadRateLimit>
{
    public static string Key => "dht_upload_rate_limit";
    public static DhtUploadRateLimit FromValue(object value) => new((int)value);
    object ISettingsEntry<DhtUploadRateLimit>.Value => Value;
}
