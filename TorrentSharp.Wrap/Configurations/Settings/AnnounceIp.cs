namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>announce_ip</c> is the ip address passed along to trackers as the <c>&amp;ip=</c> parameter. If left as the
/// default, that parameter is omitted.
/// </summary>
public sealed record AnnounceIp(string Value) : ISettingsEntry<AnnounceIp>
{
    public static string Key => "announce_ip";
    public static AnnounceIp FromValue(object value) => new((string)value);
    object ISettingsEntry<AnnounceIp>.Value => Value;
}
