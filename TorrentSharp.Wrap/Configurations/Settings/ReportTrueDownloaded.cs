namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// when set to true, the downloaded counter sent to trackers will include the actual number of payload bytes
/// downloaded including redundant bytes. If set to false, it will not include any redundancy bytes
/// </summary>
public sealed record ReportTrueDownloaded(bool Value) : ISettingsEntry<ReportTrueDownloaded>
{
    public static string Key => "report_true_downloaded";
    public static ReportTrueDownloaded FromValue(object value) => new((bool)value);
    object ISettingsEntry<ReportTrueDownloaded>.Value => Value;
}
