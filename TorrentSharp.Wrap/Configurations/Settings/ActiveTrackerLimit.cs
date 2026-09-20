namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>active_tracker_limit</c> is the max number of torrents to announce to their trackers.
/// </summary>
public sealed record ActiveTrackerLimit(int Value) : ISettingsEntry<ActiveTrackerLimit>
{
    public static string Key => "active_tracker_limit";
    public static ActiveTrackerLimit FromValue(object value) => new((int)value);
    object ISettingsEntry<ActiveTrackerLimit>.Value => Value;
}
