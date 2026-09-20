namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// max number of items the DHT will store
/// </summary>
public sealed record DhtMaxDhtItems(int Value) : ISettingsEntry<DhtMaxDhtItems>
{
    public static string Key => "dht_max_dht_items";
    public static DhtMaxDhtItems FromValue(object value) => new((int)value);
    object ISettingsEntry<DhtMaxDhtItems>.Value => Value;
}
