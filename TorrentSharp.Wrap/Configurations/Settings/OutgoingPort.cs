namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// this is the first port to use for binding outgoing connections to. This is useful for users that have routers
/// that allow QoS settings based on local port. when binding outgoing connections to specific ports,
/// <c>num_outgoing_ports</c> is the size of the range. It should be more than a few
/// </summary>
public sealed record OutgoingPort(int Value) : ISettingsEntry<OutgoingPort>
{
    public static string Key => "outgoing_port";
    public static OutgoingPort FromValue(object value) => new((int)value);
    object ISettingsEntry<OutgoingPort>.Value => Value;
}
