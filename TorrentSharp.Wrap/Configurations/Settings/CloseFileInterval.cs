namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the number of seconds between closing the file opened the longest ago. 0 means to disable the feature. The
/// purpose of this is to periodically close files to trigger the operating system flushing disk cache. Specifically
/// it has been observed to be required on windows to not have the disk cache grow indefinitely. This defaults to
/// 240 seconds on windows, and disabled on other systems.
/// </summary>
public sealed record CloseFileInterval(int Value) : ISettingsEntry<CloseFileInterval>
{
    public static string Key => "close_file_interval";
    public static CloseFileInterval FromValue(object value) => new((int)value);
    object ISettingsEntry<CloseFileInterval>.Value => Value;
}
