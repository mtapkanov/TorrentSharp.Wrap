namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>utp_min_timeout</c> is the shortest allowed uTP socket timeout, specified in milliseconds. The timeout
/// depends on the RTT of the connection, but is never smaller than this value. A connection times out when every
/// packet in a window is lost, or when a packet is lost twice in a row (i.e. the resent packet is lost as well).
/// </summary>
public sealed record UtpMinTimeout(int Value) : ISettingsEntry<UtpMinTimeout>
{
    public static string Key => "utp_min_timeout";
    public static UtpMinTimeout FromValue(object value) => new((int)value);
    object ISettingsEntry<UtpMinTimeout>.Value => Value;
}
