namespace TorrentSharp.Wrap.Enums;

/// <summary>
/// Whether peer connections use BitTorrent protocol encryption (libtorrent's <c>enc_policy</c>), applied to
/// incoming and outgoing connections alike.
/// </summary>
public enum EncryptionPolicy
{
    /// <summary>Only encrypted connections are accepted or made.</summary>
    Forced = 0,

    /// <summary>Encrypted connections are tried first, plain ones accepted too. libtorrent's default.</summary>
    Enabled = 1,

    /// <summary>Only plain connections.</summary>
    Disabled = 2,
}
