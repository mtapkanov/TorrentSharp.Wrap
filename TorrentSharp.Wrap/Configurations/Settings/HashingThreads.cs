namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>hashing_threads</c> is the number of disk I/O threads to use for piece hash verification. These threads are
/// *in addition* to the regular disk I/O threads specified by settings_pack::aio_threads. These threads are only
/// used for full checking of torrents. The hash checking done while downloading are done by the regular disk I/O
/// threads. The hasher threads do not only compute hashes, but also perform the read from disk. On storage optimal
/// for sequential access, such as hard drives, this setting should be set to 1, which is also the default.
/// </summary>
public sealed record HashingThreads(int Value) : ISettingsEntry<HashingThreads>
{
    public static string Key => "hashing_threads";
    public static HashingThreads FromValue(object value) => new((int)value);
    object ISettingsEntry<HashingThreads>.Value => Value;
}
