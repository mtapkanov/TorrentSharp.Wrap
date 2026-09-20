namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// when disabled, any tracker or web seed with an IDNA hostname (internationalized domain name) is ignored. This is
/// a security precaution to avoid various unicode encoding attacks that might happen at the application level.
/// </summary>
public sealed record AllowIdna(bool Value) : ISettingsEntry<AllowIdna>
{
    public static string Key => "allow_idna";
    public static AllowIdna FromValue(object value) => new((bool)value);
    object ISettingsEntry<AllowIdna>.Value => Value;
}
