using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TorrentSharp.Wrap.Configurations;
using TorrentSharp.Wrap.Configurations.Settings;
using TorrentSharp.Wrap.DependencyInjection;
using TorrentSharp.Wrap.Enums;

namespace TorrentSharp.Wrap.Tests;

[TestSubject(typeof(ServiceCollectionExtensions))]
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddTorrentClient_ResolvesAsSingleton()
    {
        using var provider = new ServiceCollection()
            .AddTorrentClient()
            .BuildServiceProvider();

        var first = provider.GetRequiredService<ITorrentClient>();
        var second = provider.GetRequiredService<ITorrentClient>();

        Assert.Same(first, second);
    }

    [Fact]
    public void AddTorrentClient_ConfiguresTheClientWithServicesFromTheProvider()
    {
        var marker = new Marker();
        Marker? seen = null;

        using var provider = new ServiceCollection()
            .AddSingleton(marker)
            .AddTorrentClient((services, options) =>
            {
                seen = services.GetRequiredService<Marker>();
                options.Limits(limits => limits.Connections(50));
            })
            .BuildServiceProvider();

        Assert.Null(seen);   // nothing runs until the client is first needed
        provider.GetRequiredService<ITorrentClient>();

        Assert.Same(marker, seen);
    }

    [Fact]
    public void AddTorrentClient_DisposedWithProvider()
    {
        var provider = new ServiceCollection()
            .AddTorrentClient()
            .BuildServiceProvider();

        var client = provider.GetRequiredService<ITorrentClient>();
        provider.Dispose();

        Assert.Throws<ObjectDisposedException>(() => client.AttachMagnet("magnet:?xt=urn:btih:0000000000000000000000000000000000000000"));
    }

    [Fact]
    public void FromConfiguration_AppliesOnlyThePresentKeys_AndListsReplace()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TorrentClient:Identity:UserAgent"] = "FromConfig/1.0",
                ["TorrentClient:Network:ListenInterfaces:0"] = "127.0.0.1:6001",
                ["TorrentClient:Network:ListenInterfaces:1"] = "[::1]:6001s",
                ["TorrentClient:Network:IncomingUtp"] = "false",
                ["TorrentClient:Discovery:DhtBootstrapNodes:0"] = "node.example:6881",
                ["TorrentClient:Limits:Connections"] = "77",
                ["TorrentClient:Privacy:Encryption"] = "forced",
                ["TorrentClient:Notifications:Categories"] = "Status, Storage",
                ["TorrentClient:Timeouts:Default"] = "00:00:45",
                ["TorrentClient:Storage:DefaultDownloadPath"] = "/tmp/from-config",
            })
            .Build();

        var options = new TorrentClientOptionsBuilder();
        options
            .Identity(identity => identity.UserAgent("FromCode/1.0").PeerFingerprint("-FC0100-"))
            .Network(net => net.ListenOn(System.Net.IPAddress.Any, 6881))
            .FromConfiguration(configuration.GetSection("TorrentClient"));

        Assert.Equal("FromConfig/1.0", options.Pack.Get<UserAgent>()?.Value);
        Assert.Equal("-FC0100-", options.Pack.Get<PeerFingerprint>()?.Value);
        Assert.Equal("127.0.0.1:6001,[::1]:6001s", options.Pack.Get<ListenInterfaces>()?.Value);
        Assert.False(options.Pack.Get<EnableIncomingUtp>()?.Value);
        Assert.Null(options.Pack.Get<EnableOutgoingUtp>());
        Assert.Equal("node.example:6881", options.Pack.Get<DhtBootstrapNodes>()?.Value);
        Assert.Null(options.Pack.Get<EnableDht>());
        Assert.Equal(77, options.Pack.Get<ConnectionsLimit>()?.Value);
        Assert.Equal((int)EncryptionPolicy.Forced, options.Pack.Get<InEncPolicy>()?.Value);
        Assert.Equal(NotificationCategories.Status | NotificationCategories.Storage, options.Pack.Get<AlertMask>()?.Value);
        Assert.Equal(TimeSpan.FromSeconds(45), options.DefaultTimeout);
        Assert.Equal("/tmp/from-config", options.DefaultDownloadPath);
        Assert.Null(options.IpFilterRules);
    }

    [Fact]
    public void FromConfiguration_NamesTheKeyItCannotRead()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["TorrentClient:Limits:Connections"] = "lots" })
            .Build();

        var exception = Assert.Throws<FormatException>(() =>
            new TorrentClientOptionsBuilder().FromConfiguration(configuration.GetSection("TorrentClient")));

        Assert.Contains("TorrentClient:Limits:Connections", exception.Message);
    }

    private sealed class Marker;
}
