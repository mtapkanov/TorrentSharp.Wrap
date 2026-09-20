namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>stop_tracker_timeout</c> is the number of seconds to wait when sending a stopped message before considering a
/// tracker to have timed out. This is usually shorter, to make the client quit faster. If the value is set to 0,
/// the connections to trackers with the stopped event are suppressed.
/// </summary>
public sealed record StopTrackerTimeout(int Value) : ISettingsEntry<StopTrackerTimeout>
{
    public static string Key => "stop_tracker_timeout";
    public static StopTrackerTimeout FromValue(object value) => new((int)value);
    object ISettingsEntry<StopTrackerTimeout>.Value => Value;
}
