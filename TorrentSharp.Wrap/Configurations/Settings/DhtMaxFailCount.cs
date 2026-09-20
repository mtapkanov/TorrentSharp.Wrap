namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the maximum number of failed tries to contact a node before it is removed from the routing table. If there are
/// known working nodes that are ready to replace a failing node, it will be replaced immediately, this limit is
/// only used to clear out nodes that don't have any node that can replace them.
/// </summary>
public sealed record DhtMaxFailCount(int Value) : ISettingsEntry<DhtMaxFailCount>
{
    public static string Key => "dht_max_fail_count";
    public static DhtMaxFailCount FromValue(object value) => new((int)value);
    object ISettingsEntry<DhtMaxFailCount>.Value => Value;
}
