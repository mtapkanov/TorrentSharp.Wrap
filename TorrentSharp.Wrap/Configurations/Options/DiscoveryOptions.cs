using TorrentSharp.Wrap.Configurations.Settings;

namespace TorrentSharp.Wrap.Configurations.Options;

/// <summary>How peers are found beyond trackers, and how the listen port is opened on the router.</summary>
public sealed class DiscoveryOptions
{
    private readonly SettingsPack _pack;

    internal DiscoveryOptions(SettingsPack pack) => _pack = pack;

    /// <summary>
    /// The DHT, optionally with the nodes it bootstraps from when it knows no others yet (see
    /// <see cref="ITorrentClientStartupOptions.State"/>). libtorrent: <c>enable_dht</c>, on by default.
    /// </summary>
    public DiscoveryOptions Dht(bool enabled = true, IEnumerable<string>? bootstrapNodes = null)
    {
        _pack.Set(new EnableDht(enabled));
        return bootstrapNodes is null ? this : DhtBootstrapNodes(bootstrapNodes);
    }

    /// <summary>
    /// <c>host:port</c> nodes the DHT bootstraps from. libtorrent: <c>dht_bootstrap_nodes</c>, default
    /// <c>dht.libtorrent.org:25401</c>.
    /// </summary>
    public DiscoveryOptions DhtBootstrapNodes(IEnumerable<string> nodes)
    {
        _pack.Set(new DhtBootstrapNodes(string.Join(',', nodes)));
        return this;
    }

    /// <summary>Local service discovery: finds peers on the local network. libtorrent: <c>enable_lsd</c>, on by default.</summary>
    public DiscoveryOptions Lsd(bool enabled = true)
    {
        _pack.Set(new EnableLsd(enabled));
        return this;
    }

    /// <summary>Asks the router to forward the listen port over UPnP. libtorrent: <c>enable_upnp</c>, on by default.</summary>
    public DiscoveryOptions Upnp(bool enabled = true)
    {
        _pack.Set(new EnableUpnp(enabled));
        return this;
    }

    /// <summary>Asks the router to forward the listen port over NAT-PMP. libtorrent: <c>enable_natpmp</c>, on by default.</summary>
    public DiscoveryOptions NatPmp(bool enabled = true)
    {
        _pack.Set(new EnableNatpmp(enabled));
        return this;
    }
}
