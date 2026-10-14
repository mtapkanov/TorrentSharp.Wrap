using TorrentSharp.Wrap.Configurations.Options;
using TorrentSharp.Wrap.Notifications;

namespace TorrentSharp.Wrap.Configurations;

/// <summary>
/// Collects what one <see cref="TorrentClient"/> constructor or <see cref="ITorrentClient.ChangeOptions"/> call
/// was told: the libtorrent settings in <see cref="Pack"/>, plus what the client itself keeps. Anything left
/// unset (an empty pack entry, a <c>null</c> here) is not touched.
/// </summary>
internal sealed class TorrentClientOptionsBuilder : ITorrentClientStartupOptions
{
    public SettingsPack Pack { get; } = new();

    public string? DefaultDownloadPath { get; set; }

    public bool? IncludeUnmappedEvents { get; set; }

    public TimeSpan? DefaultTimeout { get; set; }

    /// <summary>Set as soon as <see cref="IpFilter"/> is called, even with no rules - which then clears the filter.</summary>
    public IReadOnlyList<IpFilterRule>? IpFilterRules { get; set; }

    public List<EventHandler<SessionNotification>> Handlers { get; } = [];

    public byte[]? SessionState { get; private set; }

    public ITorrentClientStartupOptions Identity(Action<IdentityOptions> configure) => Run(configure, new IdentityOptions(Pack));

    public ITorrentClientStartupOptions Network(Action<NetworkOptions> configure) => Run(configure, new NetworkOptions(Pack));

    public ITorrentClientStartupOptions Discovery(Action<DiscoveryOptions> configure) => Run(configure, new DiscoveryOptions(Pack));

    public ITorrentClientStartupOptions Limits(Action<LimitsOptions> configure) => Run(configure, new LimitsOptions(Pack));

    public ITorrentClientStartupOptions Privacy(Action<PrivacyOptions> configure) => Run(configure, new PrivacyOptions(Pack));

    public ITorrentClientStartupOptions Notifications(Action<NotificationOptions> configure) => Run(configure, new NotificationOptions(this));

    public ITorrentClientStartupOptions Storage(Action<StorageOptions> configure) => Run(configure, new StorageOptions(this));

    public ITorrentClientStartupOptions Timeouts(Action<TimeoutOptions> configure) => Run(configure, new TimeoutOptions(this));

    public ITorrentClientStartupOptions IpFilter(Action<IpFilterOptions> configure) => Run(configure, new IpFilterOptions(this));

    public ITorrentClientStartupOptions Advanced(Action<SettingsPack> configure) => Run(configure, Pack);

    public ITorrentClientStartupOptions OnNotification(EventHandler<SessionNotification> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        Handlers.Add(handler);
        return this;
    }

    public ITorrentClientStartupOptions State(byte[]? state)
    {
        SessionState = state;
        return this;
    }

    private TorrentClientOptionsBuilder Run<TOptions>(Action<TOptions> configure, TOptions options)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(options);
        return this;
    }

    ITorrentClientOptions ITorrentClientOptions.Identity(Action<IdentityOptions> configure) => Identity(configure);

    ITorrentClientOptions ITorrentClientOptions.Network(Action<NetworkOptions> configure) => Network(configure);

    ITorrentClientOptions ITorrentClientOptions.Discovery(Action<DiscoveryOptions> configure) => Discovery(configure);

    ITorrentClientOptions ITorrentClientOptions.Limits(Action<LimitsOptions> configure) => Limits(configure);

    ITorrentClientOptions ITorrentClientOptions.Privacy(Action<PrivacyOptions> configure) => Privacy(configure);

    ITorrentClientOptions ITorrentClientOptions.Notifications(Action<NotificationOptions> configure) => Notifications(configure);

    ITorrentClientOptions ITorrentClientOptions.Storage(Action<StorageOptions> configure) => Storage(configure);

    ITorrentClientOptions ITorrentClientOptions.Timeouts(Action<TimeoutOptions> configure) => Timeouts(configure);

    ITorrentClientOptions ITorrentClientOptions.IpFilter(Action<IpFilterOptions> configure) => IpFilter(configure);

    ITorrentClientOptions ITorrentClientOptions.Advanced(Action<SettingsPack> configure) => Advanced(configure);
}
