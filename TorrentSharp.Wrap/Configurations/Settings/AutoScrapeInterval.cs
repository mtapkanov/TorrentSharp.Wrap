namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>auto_scrape_interval</c> is the number of seconds between scrapes of queued torrents (auto managed and paused
/// torrents). Auto managed torrents that are paused, are scraped regularly in order to keep track of their
/// downloader/seed ratio. This ratio is used to determine which torrents to seed and which to pause.
/// </summary>
public sealed record AutoScrapeInterval(int Value) : ISettingsEntry<AutoScrapeInterval>
{
    public static string Key => "auto_scrape_interval";
    public static AutoScrapeInterval FromValue(object value) => new((int)value);
    object ISettingsEntry<AutoScrapeInterval>.Value => Value;
}
