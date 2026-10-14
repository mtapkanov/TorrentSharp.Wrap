using TorrentSharp.Wrap.Configurations.Options;
using TorrentSharp.Wrap.Notifications;

namespace TorrentSharp.Wrap.Configurations;

/// <summary>
/// Everything about a <see cref="TorrentClient"/> that can be set while it runs - see
/// <see cref="ITorrentClient.ChangeOptions"/>, and <see cref="ITorrentClientStartupOptions"/> for creating one.
/// </summary>
/// <remarks>
/// Each method sets only what it names; anything never set keeps libtorrent's own default (the readme lists
/// them). A later call for the same setting overrides an earlier one.
/// </remarks>
public interface ITorrentClientOptions
{
    /// <summary>How the client presents itself to trackers and peers.</summary>
    ITorrentClientOptions Identity(Action<IdentityOptions> configure);

    /// <summary>Where the client listens, and over which transports it connects.</summary>
    ITorrentClientOptions Network(Action<NetworkOptions> configure);

    /// <summary>How peers are found beyond trackers: DHT, local service discovery, router port mapping.</summary>
    ITorrentClientOptions Discovery(Action<DiscoveryOptions> configure);

    /// <summary>Transfer rates, connection counts and queue sizes.</summary>
    ITorrentClientOptions Limits(Action<LimitsOptions> configure);

    /// <summary>Anonymous mode and protocol encryption.</summary>
    ITorrentClientOptions Privacy(Action<PrivacyOptions> configure);

    /// <summary>Which notifications libtorrent generates.</summary>
    ITorrentClientOptions Notifications(Action<NotificationOptions> configure);

    /// <summary>Where torrents are saved when attached without an explicit path.</summary>
    ITorrentClientOptions Storage(Action<StorageOptions> configure);

    /// <summary>How long the async operations wait when no timeout is passed to them.</summary>
    ITorrentClientOptions Timeouts(Action<TimeoutOptions> configure);

    /// <summary>Replaces the IP filter with the rules given here - none at all clears it.</summary>
    ITorrentClientOptions IpFilter(Action<IpFilterOptions> configure);

    /// <summary>
    /// Any other libtorrent setting, through its typed entry (see <c>Configurations/Settings</c>). Applied in
    /// order with the rest, so it can also override what a method above set.
    /// </summary>
    ITorrentClientOptions Advanced(Action<SettingsPack> configure);
}

/// <summary>
/// Configures a <see cref="TorrentClient"/> as it is created: everything in <see cref="ITorrentClientOptions"/>,
/// plus what only makes sense before the session exists. The methods are repeated here so a chain keeps
/// offering the startup-only ones.
/// </summary>
public interface ITorrentClientStartupOptions : ITorrentClientOptions
{
    /// <inheritdoc cref="ITorrentClientOptions.Identity"/>
    new ITorrentClientStartupOptions Identity(Action<IdentityOptions> configure);

    /// <inheritdoc cref="ITorrentClientOptions.Network"/>
    new ITorrentClientStartupOptions Network(Action<NetworkOptions> configure);

    /// <inheritdoc cref="ITorrentClientOptions.Discovery"/>
    new ITorrentClientStartupOptions Discovery(Action<DiscoveryOptions> configure);

    /// <inheritdoc cref="ITorrentClientOptions.Limits"/>
    new ITorrentClientStartupOptions Limits(Action<LimitsOptions> configure);

    /// <inheritdoc cref="ITorrentClientOptions.Privacy"/>
    new ITorrentClientStartupOptions Privacy(Action<PrivacyOptions> configure);

    /// <inheritdoc cref="ITorrentClientOptions.Notifications"/>
    new ITorrentClientStartupOptions Notifications(Action<NotificationOptions> configure);

    /// <inheritdoc cref="ITorrentClientOptions.Storage"/>
    new ITorrentClientStartupOptions Storage(Action<StorageOptions> configure);

    /// <inheritdoc cref="ITorrentClientOptions.Timeouts"/>
    new ITorrentClientStartupOptions Timeouts(Action<TimeoutOptions> configure);

    /// <inheritdoc cref="ITorrentClientOptions.IpFilter"/>
    new ITorrentClientStartupOptions IpFilter(Action<IpFilterOptions> configure);

    /// <inheritdoc cref="ITorrentClientOptions.Advanced"/>
    new ITorrentClientStartupOptions Advanced(Action<SettingsPack> configure);

    /// <summary>
    /// Subscribes to <see cref="ITorrentClient.NotificationRaised"/> before the session starts. A session
    /// created already listening reports its sockets and port mappings within milliseconds, and a
    /// notification nobody is subscribed to yet is gone - so anything that needs those subscribes here.
    /// </summary>
    ITorrentClientStartupOptions OnNotification(EventHandler<SessionNotification> handler);

    /// <summary>
    /// The state of a previous session, from <see cref="ITorrentClient.GetState"/>: its DHT node ids and the
    /// nodes it knew, so the DHT doesn't start over from the bootstrap nodes. <c>null</c>, empty or
    /// unreadable state is ignored.
    /// </summary>
    /// <remarks>
    /// libtorrent 2.0 only uses it if the DHT starts after the listen sockets are open, which is what
    /// happens while there are bootstrap nodes to resolve first - libtorrent's default, and
    /// <see cref="Options.DiscoveryOptions.DhtBootstrapNodes"/>. With the bootstrap nodes cleared, the DHT starts
    /// before any socket exists and the saved state is dropped.
    /// </remarks>
    ITorrentClientStartupOptions State(byte[]? state);
}
