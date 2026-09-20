namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// if the allowed encryption level is both, setting this to true will prefer RC4 if both methods are offered, plain
/// text otherwise
/// </summary>
public sealed record PreferRc4(bool Value) : ISettingsEntry<PreferRc4>
{
    public static string Key => "prefer_rc4";
    public static PreferRc4 FromValue(object value) => new((bool)value);
    object ISettingsEntry<PreferRc4>.Value => Value;
}
