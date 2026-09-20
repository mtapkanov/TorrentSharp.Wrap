namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// if true, tracker connections are made over the configured proxy, if any.
/// </summary>
public sealed record ProxyTrackerConnections(bool Value) : ISettingsEntry<ProxyTrackerConnections>
{
    public static string Key => "proxy_tracker_connections";
    public static ProxyTrackerConnections FromValue(object value) => new((bool)value);
    object ISettingsEntry<ProxyTrackerConnections>.Value => Value;
}
