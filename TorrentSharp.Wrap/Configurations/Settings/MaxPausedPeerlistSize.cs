namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>max_paused_peerlist_size</c> is the max peer list size used for torrents that are paused. This can be used to
/// save memory for paused torrents, since it's not as important for them to keep a large peer list.
/// </summary>
public sealed record MaxPausedPeerlistSize(int Value) : ISettingsEntry<MaxPausedPeerlistSize>
{
    public static string Key => "max_paused_peerlist_size";
    public static MaxPausedPeerlistSize FromValue(object value) => new((int)value);
    object ISettingsEntry<MaxPausedPeerlistSize>.Value => Value;
}
