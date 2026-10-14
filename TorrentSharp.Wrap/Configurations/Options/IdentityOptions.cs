using TorrentSharp.Wrap.Configurations.Settings;

namespace TorrentSharp.Wrap.Configurations.Options;

/// <summary>How the client presents itself to trackers and peers.</summary>
public sealed class IdentityOptions
{
    private readonly SettingsPack _pack;

    internal IdentityOptions(SettingsPack pack) => _pack = pack;

    /// <summary>Sent to trackers and web seeds as the HTTP user agent. libtorrent: <c>user_agent</c>, default <c>libtorrent/2.0.11</c>.</summary>
    public IdentityOptions UserAgent(string userAgent)
    {
        _pack.Set(new UserAgent(userAgent));
        return this;
    }

    /// <summary>The client name and version sent to peers in the extension handshake. libtorrent: <c>handshake_client_version</c>, by default the user agent.</summary>
    public IdentityOptions HandshakeClientVersion(string version)
    {
        _pack.Set(new HandshakeClientVersion(version));
        return this;
    }

    /// <summary>The start of the peer id, e.g. <c>-TS0100-</c>. libtorrent: <c>peer_fingerprint</c>, default <c>-LT20B0-</c>.</summary>
    public IdentityOptions PeerFingerprint(string fingerprint)
    {
        _pack.Set(new PeerFingerprint(fingerprint));
        return this;
    }
}
