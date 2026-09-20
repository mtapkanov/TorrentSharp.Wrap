namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// if this is true, i2p torrents are allowed to also get peers from other sources than the tracker, and connect to
/// regular IPs, not providing any anonymization. This may be useful if the user is not interested in the
/// anonymization of i2p, but still wants to be able to connect to i2p peers.
/// </summary>
public sealed record AllowI2pMixed(bool Value) : ISettingsEntry<AllowI2pMixed>
{
    public static string Key => "allow_i2p_mixed";
    public static AllowI2pMixed FromValue(object value) => new((bool)value);
    object ISettingsEntry<AllowI2pMixed>.Value => Value;
}
