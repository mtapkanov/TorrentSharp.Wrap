namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>utp_target_delay</c> is the target delay for uTP sockets in milliseconds. A high value will make uTP
/// connections more aggressive and cause longer queues in the upload bottleneck. It cannot be too low, since the
/// noise in the measurements would cause it to send too slow. <c>utp_gain_factor</c> is the number of bytes the uTP
/// congestion window can increase at the most in one RTT. If this is set too high, the congestion controller reacts
/// too hard to noise and will not be stable, if it's set too low, it will react slow to congestion and not back off
/// as fast.
/// </summary>
public sealed record UtpTargetDelay(int Value) : ISettingsEntry<UtpTargetDelay>
{
    public static string Key => "utp_target_delay";
    public static UtpTargetDelay FromValue(object value) => new((int)value);
    object ISettingsEntry<UtpTargetDelay>.Value => Value;
}
