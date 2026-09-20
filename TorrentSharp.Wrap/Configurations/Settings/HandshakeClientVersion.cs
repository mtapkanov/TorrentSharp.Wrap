namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// this is the client name and version identifier sent to peers in the handshake message. If this is an empty
/// string, the user_agent is used instead. This string must be a UTF-8 encoded unicode string.
/// </summary>
public sealed record HandshakeClientVersion(string Value) : ISettingsEntry<HandshakeClientVersion>
{
    public static string Key => "handshake_client_version";
    public static HandshakeClientVersion FromValue(object value) => new((string)value);
    object ISettingsEntry<HandshakeClientVersion>.Value => Value;
}
