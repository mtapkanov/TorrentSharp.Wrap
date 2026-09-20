namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// if true, prefer seeding torrents when determining which torrents to give active slots to. If false, give
/// preference to downloading torrents
/// </summary>
public sealed record AutoManagePreferSeeds(bool Value) : ISettingsEntry<AutoManagePreferSeeds>
{
    public static string Key => "auto_manage_prefer_seeds";
    public static AutoManagePreferSeeds FromValue(object value) => new((bool)value);
    object ISettingsEntry<AutoManagePreferSeeds>.Value => Value;
}
