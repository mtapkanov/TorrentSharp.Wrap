using System.Net;
using TorrentSharp.Wrap.Configurations.Settings;
using TorrentSharp.Wrap.Enums;
using TorrentSharp.Wrap.Utils;

namespace TorrentSharp.Wrap.Configurations.Options;

/// <summary>Where the client listens, and over which transports it connects.</summary>
public sealed class NetworkOptions
{
    private readonly SettingsPack _pack;
    private readonly List<ListenInterface> _listenInterfaces = [];

    internal NetworkOptions(SettingsPack pack) => _pack = pack;

    /// <summary>Listens on one address and port - see <see cref="ListenOn(ListenInterface[])"/>.</summary>
    public NetworkOptions ListenOn(IPAddress address, int port, ListenFlags flags = ListenFlags.None) =>
        ListenOn(new IpInterface(new IPEndPoint(address, port), flags));

    /// <summary>
    /// The interfaces to listen on. Calls within one <see cref="ITorrentClientOptions.Network"/> add up; a later
    /// <c>Network(...)</c> that calls this replaces the list. No interfaces at all means not listening. Port 0
    /// lets the system pick one. libtorrent: <c>listen_interfaces</c>, default <c>0.0.0.0:6881,[::]:6881</c>.
    /// </summary>
    public NetworkOptions ListenOn(params ListenInterface[] interfaces)
    {
        _listenInterfaces.AddRange(interfaces);
        _pack.Set(new ListenInterfaces(string.Join(',', _listenInterfaces)));
        return this;
    }

    /// <summary>Accept and make TCP connections. libtorrent: <c>enable_incoming_tcp</c> / <c>enable_outgoing_tcp</c>, both on by default.</summary>
    public NetworkOptions Tcp(bool incoming = true, bool outgoing = true)
    {
        _pack.Set(new EnableIncomingTcp(incoming)).Set(new EnableOutgoingTcp(outgoing));
        return this;
    }

    /// <summary>Accept and make uTP connections. libtorrent: <c>enable_incoming_utp</c> / <c>enable_outgoing_utp</c>, both on by default.</summary>
    public NetworkOptions Utp(bool incoming = true, bool outgoing = true)
    {
        _pack.Set(new EnableIncomingUtp(incoming)).Set(new EnableOutgoingUtp(outgoing));
        return this;
    }
}
