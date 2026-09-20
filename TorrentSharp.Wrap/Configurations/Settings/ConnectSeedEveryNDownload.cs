namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// this setting controls the priority of downloading torrents over seeding or finished torrents when it comes to
/// making peer connections. Peer connections are throttled by the connection_speed and the half-open connection
/// limit. This makes peer connections a limited resource. Torrents that still have pieces to download are
/// prioritized by default, to avoid having many seeding torrents use most of the connection attempts and only give
/// one peer every now and then to the downloading torrent. libtorrent will loop over the downloading torrents to
/// connect a peer each, and every n:th connection attempt, a finished torrent is picked to be allowed to connect to
/// a peer. This setting controls n.
/// </summary>
public sealed record ConnectSeedEveryNDownload(int Value) : ISettingsEntry<ConnectSeedEveryNDownload>
{
    public static string Key => "connect_seed_every_n_download";
    public static ConnectSeedEveryNDownload FromValue(object value) => new((int)value);
    object ISettingsEntry<ConnectSeedEveryNDownload>.Value => Value;
}
