namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// Starts and stops the UPnP service. When started, the listen port and the DHT port are attempted to be forwarded
/// on local UPnP router devices.
/// </summary>
public sealed record EnableUpnp(bool Value) : ISettingsEntry<EnableUpnp>
{
    public static string Key => "enable_upnp";
    public static EnableUpnp FromValue(object value) => new((bool)value);
    object ISettingsEntry<EnableUpnp>.Value => Value;
}
