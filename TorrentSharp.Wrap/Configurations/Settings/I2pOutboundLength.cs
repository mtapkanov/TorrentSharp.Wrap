namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// Configures the SAM session quantity of I2P inbound and outbound tunnels [1..16]. number of hops for I2P inbound
/// and outbound tunnels [0..7] Changing these will not trigger a reconnect to the SAM bridge, they will take effect
/// the next time the SAM connection is re-established (by restarting or changing i2p_hostname or i2p_port).
/// </summary>
public sealed record I2pOutboundLength(int Value) : ISettingsEntry<I2pOutboundLength>
{
    public static string Key => "i2p_outbound_length";
    public static I2pOutboundLength FromValue(object value) => new((int)value);
    object ISettingsEntry<I2pOutboundLength>.Value => Value;
}
