namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// when true, web seeds sending bad data will be banned
/// </summary>
public sealed record BanWebSeeds(bool Value) : ISettingsEntry<BanWebSeeds>
{
    public static string Key => "ban_web_seeds";
    public static BanWebSeeds FromValue(object value) => new((bool)value);
    object ISettingsEntry<BanWebSeeds>.Value => Value;
}
