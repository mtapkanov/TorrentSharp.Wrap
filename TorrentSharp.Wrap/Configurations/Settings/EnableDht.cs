namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// starts the dht node and makes the trackerless service available to torrents.
/// </summary>
public sealed record EnableDht(bool Value) : ISettingsEntry<EnableDht>
{
    public static string Key => "enable_dht";
    public static EnableDht FromValue(object value) => new((bool)value);
    object ISettingsEntry<EnableDht>.Value => Value;
}
