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
            pack.Set("user_agent", UserAgent);
        }

        // отпечаток клиента
        if (!string.IsNullOrEmpty(Fingerprint))
        {
            pack.Set("peer_fingerprint", Fingerprint);
        }

        // уведомления
        pack.Set("alert_mask", (int)NotificationCategories);

        pack.Set("anonymous_mode", PrivateMode);
        pack.Set("seeding_outgoing_connections", !BlockSeeding);

        if (MaxConnections.HasValue)
        {
            pack.Set("connections_limit", MaxConnections.Value);
        }

        if (ForceEncryption)
        {
            pack.Set("out_enc_policy", 0);
            pack.Set("in_enc_policy", 0);
        }

        return pack;
    }
}