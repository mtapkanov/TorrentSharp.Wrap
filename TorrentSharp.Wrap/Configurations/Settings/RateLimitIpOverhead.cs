namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// if set to true, the estimated TCP/IP overhead is drained from the rate limiters, to avoid exceeding the limits
/// with the total traffic
/// </summary>
public sealed record RateLimitIpOverhead(bool Value) : ISettingsEntry<RateLimitIpOverhead>
{
    public static string Key => "rate_limit_ip_overhead";
    public static RateLimitIpOverhead FromValue(object value) => new((bool)value);
    object ISettingsEntry<RateLimitIpOverhead>.Value => Value;
}
