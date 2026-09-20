namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// for auto managed torrents, these are the limits they are subject to. If there are too many torrents some of the
/// auto managed ones will be paused until some slots free up. <c>active_downloads</c> and <c>active_seeds</c>
/// controls how many active seeding and downloading torrents the queuing mechanism allows. The target number of
/// active torrents is <c>min(active_downloads + active_seeds, active_limit)</c>. <c>active_downloads</c> and
/// <c>active_seeds</c> are upper limits on the number of downloading torrents and seeding torrents respectively.
/// Setting the value to -1 means unlimited.
/// </summary>
public sealed record ActiveSeeds(int Value) : ISettingsEntry<ActiveSeeds>
{
    public static string Key => "active_seeds";
    public static ActiveSeeds FromValue(object value) => new((int)value);
    object ISettingsEntry<ActiveSeeds>.Value => Value;
}
