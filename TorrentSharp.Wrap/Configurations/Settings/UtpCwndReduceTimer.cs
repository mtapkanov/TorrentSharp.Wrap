namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// When uTP experiences packet loss, it will reduce the congestion window, and not reduce it again for this many
/// milliseconds, even if experiencing another lost packet.
/// </summary>
public sealed record UtpCwndReduceTimer(int Value) : ISettingsEntry<UtpCwndReduceTimer>
{
    public static string Key => "utp_cwnd_reduce_timer";
    public static UtpCwndReduceTimer FromValue(object value) => new((int)value);
    object ISettingsEntry<UtpCwndReduceTimer>.Value => Value;
}
