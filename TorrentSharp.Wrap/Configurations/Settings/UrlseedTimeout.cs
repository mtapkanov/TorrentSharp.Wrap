namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// same as peer_timeout, but only applies to url-seeds. this is usually set lower, because web servers are expected
/// to be more reliable.
/// </summary>
public sealed record UrlseedTimeout(int Value) : ISettingsEntry<UrlseedTimeout>
{
    public static string Key => "urlseed_timeout";
    public static UrlseedTimeout FromValue(object value) => new((int)value);
    object ISettingsEntry<UrlseedTimeout>.Value => Value;
}
