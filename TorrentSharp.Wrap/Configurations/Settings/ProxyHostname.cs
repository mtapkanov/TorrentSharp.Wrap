namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// when using a proxy, this is the hostname where the proxy is running see proxy_type. Note that when using a
/// proxy, the settings_pack::listen_interfaces setting is overridden and only a single interface is created, just
/// to contact the proxy. This means a proxy cannot be combined with SSL torrents or multiple listen interfaces.
/// This proxy listen interface will not accept incoming TCP connections, will not map ports with any gateway and
/// will not enable local service discovery. All traffic is supposed to be channeled through the proxy.
/// </summary>
public sealed record ProxyHostname(string Value) : ISettingsEntry<ProxyHostname>
{
    public static string Key => "proxy_hostname";
    public static ProxyHostname FromValue(object value) => new((string)value);
    object ISettingsEntry<ProxyHostname>.Value => Value;
}
