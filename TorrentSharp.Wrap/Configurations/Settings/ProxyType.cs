namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// proxy to use. see proxy_type_t.
/// </summary>
public sealed record ProxyType(int Value) : ISettingsEntry<ProxyType>
{
    public static string Key => "proxy_type";
    public static ProxyType FromValue(object value) => new((int)value);
    object ISettingsEntry<ProxyType>.Value => Value;
}
