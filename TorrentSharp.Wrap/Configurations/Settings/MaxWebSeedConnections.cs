namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the max number of web seeds to have connected per torrent at any given time.
/// </summary>
public sealed record MaxWebSeedConnections(int Value) : ISettingsEntry<MaxWebSeedConnections>
{
    public static string Key => "max_web_seed_connections";
    public static MaxWebSeedConnections FromValue(object value) => new((int)value);
    object ISettingsEntry<MaxWebSeedConnections>.Value => Value;
}
