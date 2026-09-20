namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// sets the upper limit on the total number of files this session will keep open. The reason why files are left
/// open at all is that some anti virus software hooks on every file close, and scans the file for viruses.
/// deferring the closing of the files will be the difference between a usable system and a completely hogged down
/// system. Most operating systems also has a limit on the total number of file descriptors a process may have open.
/// </summary>
public sealed record FilePoolSize(int Value) : ISettingsEntry<FilePoolSize>
{
    public static string Key => "file_pool_size";
    public static FilePoolSize FromValue(object value) => new((int)value);
    object ISettingsEntry<FilePoolSize>.Value => Value;
}
