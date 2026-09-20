namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// determines if connections from the same IP address as existing connections should be rejected or not. Rejecting
/// multiple connections from the same IP address will prevent abusive behavior by peers. The logic for determining
/// whether connections are to the same peer is more complicated with this enabled, and more likely to fail in some
/// edge cases. It is not recommended to enable this feature.
/// </summary>
public sealed record AllowMultipleConnectionsPerIp(bool Value) : ISettingsEntry<AllowMultipleConnectionsPerIp>
{
    public static string Key => "allow_multiple_connections_per_ip";
    public static AllowMultipleConnectionsPerIp FromValue(object value) => new((bool)value);
    object ISettingsEntry<AllowMultipleConnectionsPerIp>.Value => Value;
}
