namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>auto_manage_interval</c> is the number of seconds between the torrent queue is updated, and rotated.
/// </summary>
public sealed record AutoManageInterval(int Value) : ISettingsEntry<AutoManageInterval>
{
    public static string Key => "auto_manage_interval";
    public static AutoManageInterval FromValue(object value) => new((int)value);
    object ISettingsEntry<AutoManageInterval>.Value => Value;
}
