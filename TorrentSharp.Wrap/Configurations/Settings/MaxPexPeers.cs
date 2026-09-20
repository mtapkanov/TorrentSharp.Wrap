namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the max number of peers we accept from pex messages from a single peer. this limits the number of concurrent
/// peers any of our peers claims to be connected to. If they claim to be connected to more than this, we'll ignore
/// any peer that exceeds this limit
/// </summary>
public sealed record MaxPexPeers(int Value) : ISettingsEntry<MaxPexPeers>
{
    public static string Key => "max_pex_peers";
    public static MaxPexPeers FromValue(object value) => new((int)value);
    object ISettingsEntry<MaxPexPeers>.Value => Value;
}
