namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>udp_tracker_token_expiry</c> is the number of seconds libtorrent will keep UDP tracker connection tokens
/// around for. This is specified to be 60 seconds. The higher this value is, the fewer packets have to be sent to
/// the UDP tracker. In order for higher values to work, the tracker needs to be configured to match the expiration
/// time for tokens.
/// </summary>
public sealed record UdpTrackerTokenExpiry(int Value) : ISettingsEntry<UdpTrackerTokenExpiry>
{
    public static string Key => "udp_tracker_token_expiry";
    public static UdpTrackerTokenExpiry FromValue(object value) => new((int)value);
    object ISettingsEntry<UdpTrackerTokenExpiry>.Value => Value;
}
