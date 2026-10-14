using TorrentSharp.Wrap.Configurations.Settings;
using TorrentSharp.Wrap.Enums;

namespace TorrentSharp.Wrap.Configurations.Options;

/// <summary>Anonymous mode and protocol encryption.</summary>
public sealed class PrivacyOptions
{
    private readonly SettingsPack _pack;

    internal PrivacyOptions(SettingsPack pack) => _pack = pack;

    /// <summary>Hides the client's identity and local addresses from trackers and peers. libtorrent: <c>anonymous_mode</c>, off by default.</summary>
    public PrivacyOptions AnonymousMode(bool enabled = true)
    {
        _pack.Set(new AnonymousMode(enabled));
        return this;
    }

    /// <summary>Protocol encryption for incoming and outgoing connections. libtorrent: <c>in_enc_policy</c> / <c>out_enc_policy</c>, <see cref="EncryptionPolicy.Enabled"/> by default.</summary>
    public PrivacyOptions Encryption(EncryptionPolicy policy)
    {
        _pack.Set(new InEncPolicy((int)policy)).Set(new OutEncPolicy((int)policy));
        return this;
    }
}
