namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// when set, perform lookups in a way that is slightly more expensive, but which minimizes the amount of
/// information leaked about you.
/// </summary>
public sealed record DhtPrivacyLookups(bool Value) : ISettingsEntry<DhtPrivacyLookups>
{
    public static string Key => "dht_privacy_lookups";
    public static DhtPrivacyLookups FromValue(object value) => new((bool)value);
    object ISettingsEntry<DhtPrivacyLookups>.Value => Value;
}
