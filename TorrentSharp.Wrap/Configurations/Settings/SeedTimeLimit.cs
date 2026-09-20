namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// this is the limit on the time a torrent has been an active seed (specified in seconds) before it is considered
/// having met the seed limit criteria. See queuing_.
/// </summary>
public sealed record SeedTimeLimit(int Value) : ISettingsEntry<SeedTimeLimit>
{
    public static string Key => "seed_time_limit";
    public static SeedTimeLimit FromValue(object value) => new((int)value);
    object ISettingsEntry<SeedTimeLimit>.Value => Value;
}
