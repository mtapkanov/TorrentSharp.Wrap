namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// when enabled, tracker and web seed requests are subject to certain restrictions.
/// </summary>
public sealed record SsrfMitigation(bool Value) : ISettingsEntry<SsrfMitigation>
{
    public static string Key => "ssrf_mitigation";
    public static SsrfMitigation FromValue(object value) => new((bool)value);
    object ISettingsEntry<SsrfMitigation>.Value => Value;
}
