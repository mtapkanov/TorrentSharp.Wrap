namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// when a seeding torrent reaches either the share ratio (bytes up / bytes down) or the seed time ratio (seconds as
/// seed / seconds as downloader) or the seed time limit (seconds as seed) it is considered done, and it will leave
/// room for other torrents. These are specified as percentages. Torrents that are considered done will still be
/// allowed to be seeded, they just won't have priority anymore. For more, see queuing_.
/// </summary>
public sealed record ShareRatioLimit(int Value) : ISettingsEntry<ShareRatioLimit>
{
    public static string Key => "share_ratio_limit";
    public static ShareRatioLimit FromValue(object value) => new((int)value);
    object ISettingsEntry<ShareRatioLimit>.Value => Value;
}
