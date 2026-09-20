namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>active_dht_limit</c> is the max number of torrents to announce to the DHT.
/// </summary>
public sealed record ActiveDhtLimit(int Value) : ISettingsEntry<ActiveDhtLimit>
{
    public static string Key => "active_dht_limit";
    public static ActiveDhtLimit FromValue(object value) => new((int)value);
    object ISettingsEntry<ActiveDhtLimit>.Value => Value;
}
