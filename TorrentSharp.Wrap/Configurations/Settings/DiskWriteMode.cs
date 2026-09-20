namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// controls whether disk writes will be made through a memory mapped file or via normal write calls. This only
/// affects the mmap_disk_io. When saving to a non-local drive (network share, NFS or NAS) using memory mapped files
/// is most likely inferior. When writing to a local SSD (especially in DAX mode) using memory mapped files likely
/// gives the best performance. The values for this setting are specified as mmap_write_mode_t.
/// </summary>
public sealed record DiskWriteMode(int Value) : ISettingsEntry<DiskWriteMode>
{
    public static string Key => "disk_write_mode";
    public static DiskWriteMode FromValue(object value) => new((int)value);
    object ISettingsEntry<DiskWriteMode>.Value => Value;
}
