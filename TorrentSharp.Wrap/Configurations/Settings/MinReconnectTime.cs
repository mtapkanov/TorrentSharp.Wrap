namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the number of seconds to wait to reconnect to a peer. this time is multiplied with the failcount.
/// </summary>
public sealed record MinReconnectTime(int Value) : ISettingsEntry<MinReconnectTime>
{
    public static string Key => "min_reconnect_time";
    public static MinReconnectTime FromValue(object value) => new((int)value);
    object ISettingsEntry<MinReconnectTime>.Value => Value;
}
