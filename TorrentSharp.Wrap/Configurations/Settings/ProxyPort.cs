namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the port of the proxy server
/// </summary>
public sealed record ProxyPort(int Value) : ISettingsEntry<ProxyPort>
{
    public static string Key => "proxy_port";
    public static ProxyPort FromValue(object value) => new((int)value);
    object ISettingsEntry<ProxyPort>.Value => Value;
}
