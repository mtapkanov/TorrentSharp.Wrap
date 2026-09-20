namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// when this is true, and incoming encrypted connections are enabled, &amp;supportcrypt=1 is included in http
/// tracker announces
/// </summary>
public sealed record AnnounceCryptoSupport(bool Value) : ISettingsEntry<AnnounceCryptoSupport>
{
    public static string Key => "announce_crypto_support";
    public static AnnounceCryptoSupport FromValue(object value) => new((bool)value);
    object ISettingsEntry<AnnounceCryptoSupport>.Value => Value;
}
