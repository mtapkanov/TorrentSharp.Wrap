using TorrentSharp.Wrap.Configurations.Settings;

namespace TorrentSharp.Wrap.Configurations.Options;

/// <summary>Transfer rates, connection counts and queue sizes.</summary>
public sealed class LimitsOptions
{
    private readonly SettingsPack _pack;

    internal LimitsOptions(SettingsPack pack) => _pack = pack;

    /// <summary>Session-wide download rate, bytes per second; 0 is unlimited. libtorrent: <c>download_rate_limit</c>, default 0.</summary>
    public LimitsOptions Download(int bytesPerSecond)
    {
        _pack.Set(new DownloadRateLimit(bytesPerSecond));
        return this;
    }

    /// <summary>Session-wide upload rate, bytes per second; 0 is unlimited. libtorrent: <c>upload_rate_limit</c>, default 0.</summary>
    public LimitsOptions Upload(int bytesPerSecond)
    {
        _pack.Set(new UploadRateLimit(bytesPerSecond));
        return this;
    }

    /// <summary>Peer connections across all torrents. libtorrent: <c>connections_limit</c>, default 200.</summary>
    public LimitsOptions Connections(int maximum)
    {
        _pack.Set(new ConnectionsLimit(maximum));
        return this;
    }

    /// <summary>
    /// How many auto-managed torrents may download, seed, or be active at all at once; -1 is no limit. Torrents
    /// attached through this library are not auto-managed, so these only matter for ones made so through
    /// <see cref="ITorrentClientOptions.Advanced"/>. libtorrent: <c>active_downloads</c> 3, <c>active_seeds</c> 5,
    /// <c>active_limit</c> 500.
    /// </summary>
    public LimitsOptions Active(int? downloads = null, int? seeds = null, int? total = null)
    {
        if (downloads is { } activeDownloads)
            _pack.Set(new ActiveDownloads(activeDownloads));
        if (seeds is { } activeSeeds)
            _pack.Set(new ActiveSeeds(activeSeeds));
        if (total is { } activeTotal)
            _pack.Set(new ActiveLimit(activeTotal));
        return this;
    }

    /// <summary>Whether a torrent that is only seeding makes outgoing connections. libtorrent: <c>seeding_outgoing_connections</c>, on by default.</summary>
    public LimitsOptions SeedingOutgoingConnections(bool enabled)
    {
        _pack.Set(new SeedingOutgoingConnections(enabled));
        return this;
    }
}
