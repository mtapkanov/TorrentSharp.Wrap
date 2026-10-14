using Microsoft.Extensions.DependencyInjection;
using TorrentSharp.Wrap.Configurations;

namespace TorrentSharp.Wrap.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="ITorrentClient"/> singleton, with everything it is configured with at startup
    /// in <paramref name="configure"/>. The client - and its libtorrent session - is created on first use.
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddTorrentClient(options => options
    ///     .Identity(identity => identity.UserAgent("MyApp/1.0"))
    ///     .Network(net => net.ListenOn(IPAddress.Any, 51413))
    ///     .Discovery(discovery => discovery.Dht().Lsd())
    ///     .Notifications(notifications => notifications.Categories(NotificationCategories.Status)));
    /// </code>
    /// </example>
    public static IServiceCollection AddTorrentClient(this IServiceCollection services, Action<ITorrentClientStartupOptions>? configure = null) =>
        services.AddTorrentClient((_, options) => configure?.Invoke(options));

    /// <summary>
    /// Same as <see cref="AddTorrentClient(IServiceCollection, Action{ITorrentClientStartupOptions})"/>, for
    /// configuration that needs other services - settings kept in a database, a handler to subscribe through
    /// <see cref="ITorrentClientStartupOptions.OnNotification"/>, a saved <see cref="ITorrentClientStartupOptions.State"/>.
    /// </summary>
    public static IServiceCollection AddTorrentClient(this IServiceCollection services, Action<IServiceProvider, ITorrentClientStartupOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        services.AddSingleton<ITorrentClient>(provider => new TorrentClient(options => configure(provider, options)));

        return services;
    }
}
