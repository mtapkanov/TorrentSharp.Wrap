namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>upload_rate_limit</c> and <c>download_rate_limit</c> sets the session-global limits of upload and download
/// rate limits, in bytes per second. By default peers on the local network are not rate limited.
/// </summary>
public sealed record DownloadRateLimit(int Value) : ISettingsEntry<DownloadRateLimit>
{
    public static string Key => "download_rate_limit";
    public static DownloadRateLimit FromValue(object value) => new((int)value);
    object ISettingsEntry<DownloadRateLimit>.Value => Value;
}
