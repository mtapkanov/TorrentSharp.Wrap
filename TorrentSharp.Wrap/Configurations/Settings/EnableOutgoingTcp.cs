namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// Enables incoming and outgoing, TCP and uTP peer connections. <c>false</c> is disabled and <c>true</c> is
/// enabled. When outgoing connections are disabled, libtorrent will simply not make outgoing peer connections with
/// the specific transport protocol. Disabled incoming peer connections will simply be rejected. These options only
/// apply to peer connections, not tracker- or any other kinds of connections.
/// </summary>
public sealed record EnableOutgoingTcp(bool Value) : ISettingsEntry<EnableOutgoingTcp>
{
    public static string Key => "enable_outgoing_tcp";
    public static EnableOutgoingTcp FromValue(object value) => new((bool)value);
    object ISettingsEntry<EnableOutgoingTcp>.Value => Value;
}
