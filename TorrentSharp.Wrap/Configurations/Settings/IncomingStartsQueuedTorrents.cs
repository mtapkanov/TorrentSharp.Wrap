namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>incoming_starts_queued_torrents</c>. If a torrent has been paused by the auto managed feature in libtorrent,
/// i.e. the torrent is paused and auto managed, this feature affects whether or not it is automatically started on
/// an incoming connection. The main reason to queue torrents, is not to make them unavailable, but to save on the
/// overhead of announcing to the trackers, the DHT and to avoid spreading one's unchoke slots too thin. If a peer
/// managed to find us, even though we're no in the torrent anymore, this setting can make us start the torrent and
/// serve it.
/// </summary>
public sealed record IncomingStartsQueuedTorrents(bool Value) : ISettingsEntry<IncomingStartsQueuedTorrents>
{
    public static string Key => "incoming_starts_queued_torrents";
    public static IncomingStartsQueuedTorrents FromValue(object value) => new((bool)value);
    object ISettingsEntry<IncomingStartsQueuedTorrents>.Value => Value;
}
