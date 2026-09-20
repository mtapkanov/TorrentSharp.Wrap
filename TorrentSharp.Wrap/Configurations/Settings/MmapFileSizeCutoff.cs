namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// when using mmap_disk_io, files smaller than this number of blocks will not be memory mapped, but will use normal
/// pread/pwrite operations. This file size limit is specified in 16 kiB blocks.
/// </summary>
public sealed record MmapFileSizeCutoff(int Value) : ISettingsEntry<MmapFileSizeCutoff>
{
    public static string Key => "mmap_file_size_cutoff";
    public static MmapFileSizeCutoff FromValue(object value) => new((int)value);
    object ISettingsEntry<MmapFileSizeCutoff>.Value => Value;
}
