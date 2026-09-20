using TorrentSharp.Wrap.Enums;

namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// a bitmask combining flags from alert_category_t defining which kinds of alerts to receive
/// </summary>
public sealed record AlertMask(NotificationCategories Value) : ISettingsEntry<AlertMask>
{
    public static string Key => "alert_mask";
    public static AlertMask FromValue(object value) => new((NotificationCategories)(int)value);
    object ISettingsEntry<AlertMask>.Value => (int)Value;
}
