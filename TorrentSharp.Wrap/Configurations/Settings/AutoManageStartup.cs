namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// this is the number of seconds a torrent is considered active after it was started, regardless of upload and
/// download speed. This is so that newly started torrents are not considered inactive until they have a fair chance
/// to start downloading.
/// </summary>
public sealed record AutoManageStartup(int Value) : ISettingsEntry<AutoManageStartup>
{
    public static string Key => "auto_manage_startup";
    public static AutoManageStartup FromValue(object value) => new((int)value);
    object ISettingsEntry<AutoManageStartup>.Value => Value;
}
