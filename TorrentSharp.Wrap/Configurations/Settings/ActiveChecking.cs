namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>active_checking</c> is the limit of number of simultaneous checking torrents.
/// </summary>
public sealed record ActiveChecking(int Value) : ISettingsEntry<ActiveChecking>
{
    public static string Key => "active_checking";
    public static ActiveChecking FromValue(object value) => new((int)value);
    object ISettingsEntry<ActiveChecking>.Value => Value;
}
