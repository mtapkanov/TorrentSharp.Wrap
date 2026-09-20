namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// if this is true, the number of redundant bytes is sent to the tracker
/// </summary>
public sealed record ReportRedundantBytes(bool Value) : ISettingsEntry<ReportRedundantBytes>
{
    public static string Key => "report_redundant_bytes";
    public static ReportRedundantBytes FromValue(object value) => new((bool)value);
    object ISettingsEntry<ReportRedundantBytes>.Value => Value;
}
