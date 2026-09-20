namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// Starts and stops the internal IP table route changes notifier.
/// </summary>
public sealed record EnableIpNotifier(bool Value) : ISettingsEntry<EnableIpNotifier>
{
    public static string Key => "enable_ip_notifier";
    public static EnableIpNotifier FromValue(object value) => new((bool)value);
    object ISettingsEntry<EnableIpNotifier>.Value => Value;
}
