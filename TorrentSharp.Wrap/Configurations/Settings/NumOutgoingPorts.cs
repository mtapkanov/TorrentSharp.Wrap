namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// this is the first port to use for binding outgoing connections to. This is useful for users that have routers
/// that allow QoS settings based on local port. when binding outgoing connections to specific ports,
/// <c>num_outgoing_ports</c> is the size of the range. It should be more than a few
/// </summary>
public sealed record NumOutgoingPorts(int Value) : ISettingsEntry<NumOutgoingPorts>
{
    public static string Key => "num_outgoing_ports";
    public static NumOutgoingPorts FromValue(object value) => new((int)value);
    object ISettingsEntry<NumOutgoingPorts>.Value => Value;
}
