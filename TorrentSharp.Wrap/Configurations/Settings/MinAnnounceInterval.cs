namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// this is the minimum allowed announce interval for a tracker. This is specified in seconds and is used as a
/// sanity check on what is returned from a tracker. It mitigates hammering mis-configured trackers.
/// </summary>
public sealed record MinAnnounceInterval(int Value) : ISettingsEntry<MinAnnounceInterval>
{
    public static string Key => "min_announce_interval";
    public static MinAnnounceInterval FromValue(object value) => new((int)value);
    object ISettingsEntry<MinAnnounceInterval>.Value => Value;
}
