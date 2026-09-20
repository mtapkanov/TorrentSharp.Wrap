namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// when using a proxy, these are the credentials (if any) to use when connecting to it. see proxy_type
/// </summary>
public sealed record ProxyUsername(string Value) : ISettingsEntry<ProxyUsername>
{
    public static string Key => "proxy_username";
    public static ProxyUsername FromValue(object value) => new((string)value);
    object ISettingsEntry<ProxyUsername>.Value => Value;
}
