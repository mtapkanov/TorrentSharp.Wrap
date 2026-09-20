namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>seed_choking_algorithm</c> controls the seeding unchoke behavior. i.e. How we select which peers to unchoke
/// for seeding torrents. Since a seeding torrent isn't downloading anything, the tit-for-tat mechanism cannot be
/// used. The available options are defined in the seed_choking_algorithm_t enum.
/// </summary>
public sealed record SeedChokingAlgorithm(int Value) : ISettingsEntry<SeedChokingAlgorithm>
{
    public static string Key => "seed_choking_algorithm";
    public static SeedChokingAlgorithm FromValue(object value) => new((int)value);
    object ISettingsEntry<SeedChokingAlgorithm>.Value => Value;
}
