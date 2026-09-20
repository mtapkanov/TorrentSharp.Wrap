namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// if this setting is true, torrents with a very high availability of pieces (and seeds) are downloaded
/// sequentially. This is more efficient for the disk I/O. With many seeds, the download order is unlikely to matter
/// anyway
/// </summary>
public sealed record AutoSequential(bool Value) : ISettingsEntry<AutoSequential>
{
    public static string Key => "auto_sequential";
    public static AutoSequential FromValue(object value) => new((bool)value);
    object ISettingsEntry<AutoSequential>.Value => Value;
}
