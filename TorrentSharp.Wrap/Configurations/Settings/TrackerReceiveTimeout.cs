namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>tracker_receive_timeout</c> is the number of seconds to wait to receive any data from the tracker. If no data
/// is received for this number of seconds, the tracker will be considered as having timed out. If a tracker is
/// down, this is the kind of timeout that will occur.
/// </summary>
public sealed record TrackerReceiveTimeout(int Value) : ISettingsEntry<TrackerReceiveTimeout>
{
    public static string Key => "tracker_receive_timeout";
    public static TrackerReceiveTimeout FromValue(object value) => new((int)value);
    object ISettingsEntry<TrackerReceiveTimeout>.Value => Value;
}
