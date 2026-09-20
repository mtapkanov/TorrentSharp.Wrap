namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>announce_to_all_tiers</c> also controls how multi tracker torrents are treated. When this is set to true, one
/// tracker from each tier is announced to. This is the uTorrent behavior. To be compliant with the Multi-tracker
/// specification, set it to false.
/// </summary>
public sealed record AnnounceToAllTiers(bool Value) : ISettingsEntry<AnnounceToAllTiers>
{
    public static string Key => "announce_to_all_tiers";
    public static AnnounceToAllTiers FromValue(object value) => new((bool)value);
    object ISettingsEntry<AnnounceToAllTiers>.Value => Value;
}
