namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// this is the fingerprint for the client. It will be used as the prefix to the peer_id. If this is 20 bytes (or
/// longer) it will be truncated to 20 bytes and used as the entire peer-id
/// </summary>
public sealed record PeerFingerprint(string Value) : ISettingsEntry<PeerFingerprint>
{
    public static string Key => "peer_fingerprint";
    public static PeerFingerprint FromValue(object value) => new((string)value);
    object ISettingsEntry<PeerFingerprint>.Value => Value;
}
