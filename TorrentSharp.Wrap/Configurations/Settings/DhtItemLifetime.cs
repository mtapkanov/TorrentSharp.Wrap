namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the number of seconds a immutable/mutable item will be expired. default is 0, means never expires.
/// </summary>
public sealed record DhtItemLifetime(int Value) : ISettingsEntry<DhtItemLifetime>
{
    public static string Key => "dht_item_lifetime";
    public static DhtItemLifetime FromValue(object value) => new((int)value);
    object ISettingsEntry<DhtItemLifetime>.Value => Value;
}
