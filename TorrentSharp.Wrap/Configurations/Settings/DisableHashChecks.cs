namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// when set to true, all data downloaded from peers will be assumed to be correct, and not tested to match the
/// hashes in the torrent this is only useful for simulation and testing purposes (typically combined with
/// disabled_storage)
/// </summary>
public sealed record DisableHashChecks(bool Value) : ISettingsEntry<DisableHashChecks>
{
    public static string Key => "disable_hash_checks";
    public static DisableHashChecks FromValue(object value) => new((bool)value);
    object ISettingsEntry<DisableHashChecks>.Value => Value;
}
