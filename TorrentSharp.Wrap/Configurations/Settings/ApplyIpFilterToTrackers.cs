namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>apply_ip_filter_to_trackers</c> determines whether the IP filter applies to trackers as well as peers. If
/// this is set to false, trackers are exempt from the IP filter (if there is one). If no IP filter is set, this
/// setting is irrelevant.
/// </summary>
public sealed record ApplyIpFilterToTrackers(bool Value) : ISettingsEntry<ApplyIpFilterToTrackers>
{
    public static string Key => "apply_ip_filter_to_trackers";
    public static ApplyIpFilterToTrackers FromValue(object value) => new((bool)value);
    object ISettingsEntry<ApplyIpFilterToTrackers>.Value => Value;
}
