namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// when using a proxy, these are the credentials (if any) to use when connecting to it. see proxy_type
/// </summary>
public sealed record ProxyPassword(string Value) : ISettingsEntry<ProxyPassword>
{
    public static string Key => "proxy_password";
    public static ProxyPassword FromValue(object value) => new((string)value);
    object ISettingsEntry<ProxyPassword>.Value => Value;
}
