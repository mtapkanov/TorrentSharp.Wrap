namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// determines how files are opened when they're in read only mode versus read and write mode. The options are:
/// </summary>
public sealed record DiskIoWriteMode(int Value) : ISettingsEntry<DiskIoWriteMode>
{
    public static string Key => "disk_io_write_mode";
    public static DiskIoWriteMode FromValue(object value) => new((int)value);
    object ISettingsEntry<DiskIoWriteMode>.Value => Value;
}
