namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the number of concurrent search request the node will send when announcing and refreshing the routing table.
/// This parameter is called alpha in the kademlia paper
/// </summary>
public sealed record DhtSearchBranching(int Value) : ISettingsEntry<DhtSearchBranching>
{
    public static string Key => "dht_search_branching";
    public static DhtSearchBranching FromValue(object value) => new((int)value);
    object ISettingsEntry<DhtSearchBranching>.Value => Value;
}
