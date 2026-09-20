namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// when set, the other nodes won't keep this node in their routing tables, it's meant for low-power and/or
/// ephemeral devices that cannot support the DHT, it is also useful for mobile devices which are sensitive to
/// network traffic and battery life. this node no longer responds to 'query' messages, and will place a 'ro' key
/// (value = 1) in the top-level message dictionary of outgoing query messages.
/// </summary>
public sealed record DhtReadOnly(bool Value) : ISettingsEntry<DhtReadOnly>
{
    public static string Key => "dht_read_only";
    public static DhtReadOnly FromValue(object value) => new((bool)value);
    object ISettingsEntry<DhtReadOnly>.Value => Value;
}
