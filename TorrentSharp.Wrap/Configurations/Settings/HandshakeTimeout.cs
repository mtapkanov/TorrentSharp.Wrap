namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the number of seconds to wait for a handshake response from a peer. If no response is received within this time,
/// the peer is disconnected.
/// </summary>
public sealed record HandshakeTimeout(int Value) : ISettingsEntry<HandshakeTimeout>
{
    public static string Key => "handshake_timeout";
    public static HandshakeTimeout FromValue(object value) => new((int)value);
    object ISettingsEntry<HandshakeTimeout>.Value => Value;
}
