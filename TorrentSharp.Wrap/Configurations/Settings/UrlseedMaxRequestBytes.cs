namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// The maximum request range of an url seed in bytes. This value defines the largest possible sequential web seed
/// request. Lower values are possible but will be ignored if they are lower then piece size. This value should be
/// related to your download speed to prevent libtorrent from creating too many expensive http requests per second.
/// You can select a value as high as you want but keep in mind that libtorrent can't create parallel requests if
/// the first request did already select the whole file. If you combine bittorrent seeds with web seeds and pick
/// strategies like rarest first you may find your web seed requests split into smaller parts because we don't
/// download already picked pieces twice.
/// </summary>
public sealed record UrlseedMaxRequestBytes(int Value) : ISettingsEntry<UrlseedMaxRequestBytes>
{
    public static string Key => "urlseed_max_request_bytes";
    public static UrlseedMaxRequestBytes FromValue(object value) => new((int)value);
    object ISettingsEntry<UrlseedMaxRequestBytes>.Value => Value;
}
