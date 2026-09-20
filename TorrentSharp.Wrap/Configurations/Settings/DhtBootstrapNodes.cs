namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// This is a comma-separated list of IP port-pairs. They will be added to the DHT node (if it's enabled) as back-up
/// nodes in case we don't know of any.
/// </summary>
public sealed record DhtBootstrapNodes(string Value) : ISettingsEntry<DhtBootstrapNodes>
{
    public static string Key => "dht_bootstrap_nodes";
    public static DhtBootstrapNodes FromValue(object value) => new((string)value);
    object ISettingsEntry<DhtBootstrapNodes>.Value => Value;
}
