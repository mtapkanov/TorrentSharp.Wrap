namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>optimistic_disk_retry</c> is the number of seconds from a disk write errors occur on a torrent until
/// libtorrent will take it out of the upload mode, to test if the error condition has been fixed.
/// </summary>
public sealed record OptimisticDiskRetry(int Value) : ISettingsEntry<OptimisticDiskRetry>
{
    public static string Key => "optimistic_disk_retry";
    public static OptimisticDiskRetry FromValue(object value) => new((int)value);
    object ISettingsEntry<OptimisticDiskRetry>.Value => Value;
}
