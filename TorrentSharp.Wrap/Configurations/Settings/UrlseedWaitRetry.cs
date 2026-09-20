namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// number of seconds until a new retry of a url-seed takes place. Default retry value for http-seeds that don't
/// provide a valid <c>retry-after</c> header.
/// </summary>
public sealed record UrlseedWaitRetry(int Value) : ISettingsEntry<UrlseedWaitRetry>
{
    public static string Key => "urlseed_wait_retry";
    public static UrlseedWaitRetry FromValue(object value) => new((int)value);
    object ISettingsEntry<UrlseedWaitRetry>.Value => Value;
}
