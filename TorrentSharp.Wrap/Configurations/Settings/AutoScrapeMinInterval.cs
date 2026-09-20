namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>auto_scrape_min_interval</c> is the minimum number of seconds between any automatic scrape (regardless of
/// torrent). In case there are a large number of paused auto managed torrents, this puts a limit on how often a
/// scrape request is sent.
/// </summary>
public sealed record AutoScrapeMinInterval(int Value) : ISettingsEntry<AutoScrapeMinInterval>
{
    public static string Key => "auto_scrape_min_interval";
    public static AutoScrapeMinInterval FromValue(object value) => new((int)value);
    object ISettingsEntry<AutoScrapeMinInterval>.Value => Value;
}
