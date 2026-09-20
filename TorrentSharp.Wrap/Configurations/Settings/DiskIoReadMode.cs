namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// determines how files are opened when they're in read only mode versus read and write mode. The options are:
/// </summary>
public sealed record DiskIoReadMode(int Value) : ISettingsEntry<DiskIoReadMode>
{
    public static string Key => "disk_io_read_mode";
    public static DiskIoReadMode FromValue(object value) => new((int)value);
    object ISettingsEntry<DiskIoReadMode>.Value => Value;
}
