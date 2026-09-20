namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// Enables incoming and outgoing, TCP and uTP peer connections. <c>false</c> is disabled and <c>true</c> is
/// enabled. When outgoing connections are disabled, libtorrent will simply not make outgoing peer connections with
/// the specific transport protocol. Disabled incoming peer connections will simply be rejected. These options only
/// apply to peer connections, not tracker- or any other kinds of connections.
/// </summary>
public sealed record EnableIncomingTcp(bool Value) : ISettingsEntry<EnableIncomingTcp>
{
    public static string Key => "enable_incoming_tcp";
    public static EnableIncomingTcp FromValue(object value) => new((bool)value);
    object ISettingsEntry<EnableIncomingTcp>.Value => Value;
}
