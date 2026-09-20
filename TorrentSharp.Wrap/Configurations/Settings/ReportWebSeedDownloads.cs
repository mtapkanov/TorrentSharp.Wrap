namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// specifies whether downloads from web seeds is reported to the tracker or not. Turning it off also excludes web
/// seed traffic from other stats and download rate reporting via the libtorrent API.
/// </summary>
public sealed record ReportWebSeedDownloads(bool Value) : ISettingsEntry<ReportWebSeedDownloads>
{
    public static string Key => "report_web_seed_downloads";
    public static ReportWebSeedDownloads FromValue(object value) => new((bool)value);
    object ISettingsEntry<ReportWebSeedDownloads>.Value => Value;
}
