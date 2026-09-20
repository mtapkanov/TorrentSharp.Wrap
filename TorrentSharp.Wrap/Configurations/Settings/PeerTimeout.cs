namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>peer_timeout</c> is the number of seconds the peer connection should wait (for any activity on the peer
/// connection) before closing it due to time out. 120 seconds is specified in the protocol specification. After
/// half the time out, a keep alive message is sent.
/// </summary>
public sealed record PeerTimeout(int Value) : ISettingsEntry<PeerTimeout>
{
    public static string Key => "peer_timeout";
    public static PeerTimeout FromValue(object value) => new((int)value);
    object ISettingsEntry<PeerTimeout>.Value => Value;
}
