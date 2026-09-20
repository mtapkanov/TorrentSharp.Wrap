namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>tracker_completion_timeout</c> is the number of seconds the tracker connection will wait from when it sent
/// the request until it considers the tracker to have timed-out.
/// </summary>
public sealed record TrackerCompletionTimeout(int Value) : ISettingsEntry<TrackerCompletionTimeout>
{
    public static string Key => "tracker_completion_timeout";
    public static TrackerCompletionTimeout FromValue(object value) => new((int)value);
    object ISettingsEntry<TrackerCompletionTimeout>.Value => Value;
}
