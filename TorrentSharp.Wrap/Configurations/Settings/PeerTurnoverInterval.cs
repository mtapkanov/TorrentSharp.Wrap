namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// peer_turnover is the percentage of peers to disconnect every turnover peer_turnover_interval (if we're at the
/// peer limit), this is specified in percent when we are connected to more than limit * peer_turnover_cutoff peers
/// disconnect peer_turnover fraction of the peers. It is specified in percent peer_turnover_interval is the
/// interval (in seconds) between optimistic disconnects if the disconnects happen and how many peers are
/// disconnected is controlled by peer_turnover and peer_turnover_cutoff
/// </summary>
public sealed record PeerTurnoverInterval(int Value) : ISettingsEntry<PeerTurnoverInterval>
{
    public static string Key => "peer_turnover_interval";
    public static PeerTurnoverInterval FromValue(object value) => new((int)value);
    object ISettingsEntry<PeerTurnoverInterval>.Value => Value;
}
