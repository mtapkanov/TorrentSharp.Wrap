namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// when this is true, libtorrent will not attempt to make outgoing connections to peers whose port is &lt; 1024.
/// This is a safety precaution to avoid being part of a DDoS attack
/// </summary>
public sealed record NoConnectPrivilegedPorts(bool Value) : ISettingsEntry<NoConnectPrivilegedPorts>
{
    public static string Key => "no_connect_privileged_ports";
    public static NoConnectPrivilegedPorts FromValue(object value) => new((bool)value);
    object ISettingsEntry<NoConnectPrivilegedPorts>.Value => Value;
}
