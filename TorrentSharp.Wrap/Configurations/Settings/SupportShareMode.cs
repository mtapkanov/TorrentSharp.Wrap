namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// if false, prevents libtorrent to advertise share-mode support
/// </summary>
public sealed record SupportShareMode(bool Value) : ISettingsEntry<SupportShareMode>
{
    public static string Key => "support_share_mode";
    public static SupportShareMode FromValue(object value) => new((bool)value);
    object ISettingsEntry<SupportShareMode>.Value => Value;
}
