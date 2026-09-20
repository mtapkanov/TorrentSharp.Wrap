namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>active_limit</c> is a hard limit on the number of active (auto managed) torrents. This limit also applies to
/// slow torrents.
/// </summary>
public sealed record ActiveLimit(int Value) : ISettingsEntry<ActiveLimit>
{
    public static string Key => "active_limit";
    public static ActiveLimit FromValue(object value) => new((int)value);
    object ISettingsEntry<ActiveLimit>.Value => Value;
}
