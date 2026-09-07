using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TorrentSharp.Wrap.Configurations;

namespace TorrentSharp.Wrap.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a single <see cref="TorrentClient"/> instance backed by the options pattern.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configureOptions">Configures the <see cref="TorrentClientConfig"/> used to build the client</param>
    public static IServiceCollection AddTorrentClient(this IServiceCollection services, Action<TorrentClientConfig>? configureOptions = null)
    {
        var optionsBuilder = services.AddOptions<TorrentClientConfig>();

        if (configureOptions is not null)
            optionsBuilder.Configure(configureOptions);

        services.AddSingleton(sp => new TorrentClient(sp.GetRequiredService<IOptions<TorrentClientConfig>>().Value));

        return services;
    }
}
