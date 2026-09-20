namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>max_peerlist_size</c> is the maximum number of peers in the list of known peers. These peers are not
/// necessarily connected, so this number should be much greater than the maximum number of connected peers. Peers
/// are evicted from the cache when the list grows passed 90% of this limit, and once the size hits the limit, peers
/// are no longer added to the list. If this limit is set to 0, there is no limit on how many peers we'll keep in
/// the peer list.
/// </summary>
public sealed record MaxPeerlistSize(int Value) : ISettingsEntry<MaxPeerlistSize>
{
    public static string Key => "max_peerlist_size";
    public static MaxPeerlistSize FromValue(object value) => new((int)value);
    object ISettingsEntry<MaxPeerlistSize>.Value => Value;
}
