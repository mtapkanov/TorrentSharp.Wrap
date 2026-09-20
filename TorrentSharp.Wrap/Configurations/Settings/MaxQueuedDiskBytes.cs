namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>max_queued_disk_bytes</c> is the maximum number of bytes, to be written to disk, that can wait in the disk
/// I/O thread queue. This queue is only for waiting for the disk I/O thread to receive the job and either write it
/// to disk or insert it in the write cache. When this limit is reached, the peer connections will stop reading data
/// from their sockets, until the disk thread catches up. Setting this too low will severely limit your download
/// rate.
/// </summary>
public sealed record MaxQueuedDiskBytes(int Value) : ISettingsEntry<MaxQueuedDiskBytes>
{
    public static string Key => "max_queued_disk_bytes";
    public static MaxQueuedDiskBytes FromValue(object value) => new((int)value);
    object ISettingsEntry<MaxQueuedDiskBytes>.Value => Value;
}
