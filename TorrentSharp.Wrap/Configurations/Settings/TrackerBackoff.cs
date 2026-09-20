namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>tracker_backoff</c> determines how aggressively to back off from retrying failing trackers. This value
/// determines *x* in the following formula, determining the number of seconds to wait until the next retry:
/// </summary>
public sealed record TrackerBackoff(int Value) : ISettingsEntry<TrackerBackoff>
{
    public static string Key => "tracker_backoff";
    public static TrackerBackoff FromValue(object value) => new((int)value);
    object ISettingsEntry<TrackerBackoff>.Value => Value;
}
