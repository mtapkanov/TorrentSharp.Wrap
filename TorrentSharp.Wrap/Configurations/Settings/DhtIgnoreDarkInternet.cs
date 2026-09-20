namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// ignore DHT messages from parts of the internet we wouldn't expect to see any traffic from
/// </summary>
public sealed record DhtIgnoreDarkInternet(bool Value) : ISettingsEntry<DhtIgnoreDarkInternet>
{
    public static string Key => "dht_ignore_dark_internet";
    public static DhtIgnoreDarkInternet FromValue(object value) => new((bool)value);
    object ISettingsEntry<DhtIgnoreDarkInternet>.Value => Value;
}
