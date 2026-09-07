using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using TorrentSharp.Wrap.DependencyInjection;

namespace TorrentSharp.Wrap.Tests;

[TestSubject(typeof(ServiceCollectionExtensions))]
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddTorrentClient_ResolvesAsSingleton()
    {
        var provider = new ServiceCollection()
            .AddTorrentClient()
            .BuildServiceProvider();

        var first = provider.GetRequiredService<TorrentClient>();
        var second = provider.GetRequiredService<TorrentClient>();

        Assert.Same(first, second);
    }

    [Fact]
    public void AddTorrentClient_AppliesConfigureOptions()
    {
        var provider = new ServiceCollection()
            .AddTorrentClient(config => config.MaxConnections = 50)
            .BuildServiceProvider();

        var exception = Record.Exception(() => provider.GetRequiredService<TorrentClient>());

        Assert.Null(exception);
    }

    [Fact]
    public void AddTorrentClient_DisposedWithProvider()
    {
        var provider = new ServiceCollection()
            .AddTorrentClient()
            .BuildServiceProvider();

        var client = provider.GetRequiredService<TorrentClient>();
        provider.Dispose();

        Assert.Throws<ObjectDisposedException>(() => client.AttachMagnet("magnet:?xt=urn:btih:0000000000000000000000000000000000000000"));
    }
}
