namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>announce_to_all_trackers</c> controls how multi tracker torrents are treated. If this is set to true, all
/// trackers in the same tier are announced to in parallel. If all trackers in tier 0 fails, all trackers in tier 1
/// are announced as well. If it's set to false, the behavior is as defined by the multi tracker specification.
/// </summary>
public sealed record AnnounceToAllTrackers(bool Value) : ISettingsEntry<AnnounceToAllTrackers>
{
    public static string Key => "announce_to_all_trackers";
    public static AnnounceToAllTrackers FromValue(object value) => new((bool)value);
    object ISettingsEntry<AnnounceToAllTrackers>.Value => Value;
}
