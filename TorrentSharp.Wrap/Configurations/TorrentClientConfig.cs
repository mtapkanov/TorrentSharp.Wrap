using TorrentSharp.Wrap.Configurations.Settings;
using TorrentSharp.Wrap.Enums;

namespace TorrentSharp.Wrap.Configurations;

public class TorrentClientConfig
{
    public string? UserAgent { get; set; }

    public string? Fingerprint { get; set; }

    public bool PrivateMode { get; set; }
    public bool BlockSeeding { get; set; }
    public bool ForceEncryption { get; set; }

    /// <summary>
    /// Notification categories the client should subscribe to.
    /// A subset is always enabled regardless of this value, since the client depends on them internally.
    /// </summary>
    public NotificationCategories NotificationCategories { get; set; }

    public int? MaxConnections { get; set; } = 200;

    public SettingsPack Build()
    {
        var pack = new SettingsPack();

        // user-agent клиента
        if (!string.IsNullOrEmpty(UserAgent))
        {
            pack.Set(new UserAgent(UserAgent));
        }

        // отпечаток клиента
        if (!string.IsNullOrEmpty(Fingerprint))
        {
            pack.Set(new PeerFingerprint(Fingerprint));
        }

        // уведомления
        pack.Set(new AlertMask(NotificationCategories));

        pack.Set(new AnonymousMode(PrivateMode));
        pack.Set(new SeedingOutgoingConnections(!BlockSeeding));

        if (MaxConnections.HasValue)
        {
            pack.Set(new ConnectionsLimit(MaxConnections.Value));
        }

        if (ForceEncryption)
        {
            pack.Set(new OutEncPolicy(0));
            pack.Set(new InEncPolicy(0));
        }

        return pack;
    }
}