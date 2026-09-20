namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// if true, hostname lookups are done via the configured proxy (if any). This is only supported by SOCKS5 and HTTP.
/// </summary>
public sealed record ProxyHostnames(bool Value) : ISettingsEntry<ProxyHostnames>
{
    public static string Key => "proxy_hostnames";
    public static ProxyHostnames FromValue(object value) => new((bool)value);
    object ISettingsEntry<ProxyHostnames>.Value => Value;
}
