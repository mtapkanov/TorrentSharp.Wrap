namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// When using a SOCKS5 proxy, UDP traffic is routed through the proxy by sending a UDP ASSOCIATE command. If this
/// option is true, the UDP ASSOCIATE command will include the IP address and listen port to the local UDP socket.
/// This indicates to the proxy which source endpoint to expect our packets from. The benefit is that incoming
/// packets can be forwarded correctly, before any outgoing packets are sent. The risk is that if there's a NAT
/// between the client and the proxy, the IP address specified in the protocol may not be valid from the proxy's
/// point of view.
/// </summary>
public sealed record Socks5UdpSendLocalEp(bool Value) : ISettingsEntry<Socks5UdpSendLocalEp>
{
    public static string Key => "socks5_udp_send_local_ep";
    public static Socks5UdpSendLocalEp FromValue(object value) => new((bool)value);
    object ISettingsEntry<Socks5UdpSendLocalEp>.Value => Value;
}
