namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// if <c>dont_count_slow_torrents</c> is true, torrents without any payload transfers are not subject to the
/// <c>active_seeds</c> and <c>active_downloads</c> limits. This is intended to make it more likely to utilize all
/// available bandwidth, and avoid having torrents that don't transfer anything block the active slots.
/// </summary>
public sealed record DontCountSlowTorrents(bool Value) : ISettingsEntry<DontCountSlowTorrents>
{
    public static string Key => "dont_count_slow_torrents";
    public static DontCountSlowTorrents FromValue(object value) => new((bool)value);
    object ISettingsEntry<DontCountSlowTorrents>.Value => Value;
}
