namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>use_dht_as_fallback</c> determines how the DHT is used. If this is true, the DHT will only be used for
/// torrents where all trackers in its tracker list has failed. Either by an explicit error message or a time out.
/// If this is false, the DHT is used regardless of if the trackers fail or not.
/// </summary>
public sealed record UseDhtAsFallback(bool Value) : ISettingsEntry<UseDhtAsFallback>
{
    public static string Key => "use_dht_as_fallback";
    public static UseDhtAsFallback FromValue(object value) => new((bool)value);
    object ISettingsEntry<UseDhtAsFallback>.Value => Value;
}
