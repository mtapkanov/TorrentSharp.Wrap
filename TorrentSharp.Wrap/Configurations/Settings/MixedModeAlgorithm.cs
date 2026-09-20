namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// The <c>mixed_mode_algorithm</c> determines how to treat TCP connections when there are uTP connections. Since
/// uTP is designed to yield to TCP, there's an inherent problem when using swarms that have both TCP and uTP
/// connections. If nothing is done, uTP connections would often be starved out for bandwidth by the TCP
/// connections. This mode is <c>prefer_tcp</c>. The <c>peer_proportional</c> mode simply looks at the current
/// throughput and rate limits all TCP connections to their proportional share based on how many of the connections
/// are TCP. This works best if uTP connections are not rate limited by the global rate limiter (which they aren't
/// by default).
/// </summary>
public sealed record MixedModeAlgorithm(int Value) : ISettingsEntry<MixedModeAlgorithm>
{
    public static string Key => "mixed_mode_algorithm";
    public static MixedModeAlgorithm FromValue(object value) => new((int)value);
    object ISettingsEntry<MixedModeAlgorithm>.Value => Value;
}
