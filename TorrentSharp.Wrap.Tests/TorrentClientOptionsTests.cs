using System.Net;
using JetBrains.Annotations;
using TorrentSharp.Wrap.Configurations;
using TorrentSharp.Wrap.Configurations.Settings;
using TorrentSharp.Wrap.Enums;
using TorrentSharp.Wrap.Utils;

namespace TorrentSharp.Wrap.Tests;

/// <summary>Each fluent method writes exactly its own libtorrent keys, and nothing is written unasked.</summary>
[TestSubject(typeof(ITorrentClientOptions))]
public class TorrentClientOptionsTests
{
    [Fact]
    public void NothingConfigured_WritesNothing()
    {
        var options = new TorrentClientOptionsBuilder();

        Assert.True(options.Pack.IsEmpty);
        Assert.Null(options.DefaultDownloadPath);
        Assert.Null(options.DefaultTimeout);
        Assert.Null(options.IncludeUnmappedEvents);
        Assert.Null(options.IpFilterRules);
        Assert.Empty(options.Handlers);
        Assert.Null(options.SessionState);
    }

    [Fact]
    public void Identity_WritesItsKeys()
    {
        var pack = Configure(o => o.Identity(id => id.UserAgent("tsw/1.0").HandshakeClientVersion("tsw 1.0").PeerFingerprint("-TS0100-")));

        Assert.Equal("tsw/1.0", pack.Get<UserAgent>()?.Value);
        Assert.Equal("tsw 1.0", pack.Get<HandshakeClientVersion>()?.Value);
        Assert.Equal("-TS0100-", pack.Get<PeerFingerprint>()?.Value);
    }

    [Fact]
    public void Network_ListenOnAddsUpWithinOneCall_AndALaterCallReplaces()
    {
        var pack = Configure(o => o
            .Network(net => net.ListenOn(IPAddress.Any, 6881).ListenOn(new IpInterface(new IPEndPoint(IPAddress.IPv6Loopback, 6881), ListenFlags.Ssl)))
            .Network(net => net.Utp(incoming: false)));

        Assert.Equal("0.0.0.0:6881,[::1]:6881s", pack.Get<ListenInterfaces>()?.Value);
        Assert.False(pack.Get<EnableIncomingUtp>()?.Value);
        Assert.True(pack.Get<EnableOutgoingUtp>()?.Value);
        Assert.Null(pack.Get<EnableIncomingTcp>());

        var replaced = Configure(o => o
            .Network(net => net.ListenOn(IPAddress.Any, 6881))
            .Network(net => net.ListenOn(IPAddress.Loopback, 7000)));

        Assert.Equal("127.0.0.1:7000", replaced.Get<ListenInterfaces>()?.Value);
    }

    [Fact]
    public void Discovery_WritesItsKeys_AndBootstrapNodesOnlyWhenGiven()
    {
        var pack = Configure(o => o.Discovery(d => d.Dht(bootstrapNodes: ["a:1", "b:2"]).Lsd(false).Upnp(false).NatPmp()));

        Assert.True(pack.Get<EnableDht>()?.Value);
        Assert.Equal("a:1,b:2", pack.Get<DhtBootstrapNodes>()?.Value);
        Assert.False(pack.Get<EnableLsd>()?.Value);
        Assert.False(pack.Get<EnableUpnp>()?.Value);
        Assert.True(pack.Get<EnableNatpmp>()?.Value);

        Assert.Null(Configure(o => o.Discovery(d => d.Dht(false))).Get<DhtBootstrapNodes>());
    }

    [Fact]
    public void Limits_WritesItsKeys_AndActiveOnlyThoseGiven()
    {
        var pack = Configure(o => o.Limits(l => l.Download(100).Upload(200).Connections(300).Active(seeds: -1).SeedingOutgoingConnections(false)));

        Assert.Equal(100, pack.Get<DownloadRateLimit>()?.Value);
        Assert.Equal(200, pack.Get<UploadRateLimit>()?.Value);
        Assert.Equal(300, pack.Get<ConnectionsLimit>()?.Value);
        Assert.Equal(-1, pack.Get<ActiveSeeds>()?.Value);
        Assert.Null(pack.Get<ActiveDownloads>());
        Assert.Null(pack.Get<ActiveLimit>());
        Assert.False(pack.Get<SeedingOutgoingConnections>()?.Value);
    }

    [Theory]
    [InlineData(EncryptionPolicy.Forced, 0)]
    [InlineData(EncryptionPolicy.Enabled, 1)]
    [InlineData(EncryptionPolicy.Disabled, 2)]
    public void Privacy_MapsEncryptionToLibtorrentsPolicyValues(EncryptionPolicy policy, int expected)
    {
        var pack = Configure(o => o.Privacy(p => p.AnonymousMode().Encryption(policy)));

        Assert.True(pack.Get<AnonymousMode>()?.Value);
        Assert.Equal(expected, pack.Get<InEncPolicy>()?.Value);
        Assert.Equal(expected, pack.Get<OutEncPolicy>()?.Value);
    }

    [Fact]
    public void ClientLevelOptions_AreKeptOutsideThePack()
    {
        var options = new TorrentClientOptionsBuilder();
        options
            .Notifications(n => n.Categories(NotificationCategories.Status).IncludeUnmapped())
            .Storage(s => s.DefaultDownloadPath("/tmp/tsw"))
            .Timeouts(t => t.Default(TimeSpan.FromSeconds(5)))
            .IpFilter(f => f.Block("10.0.0.0", "10.255.255.255"));

        Assert.Equal(NotificationCategories.Status, options.Pack.Get<AlertMask>()?.Value);
        Assert.True(options.IncludeUnmappedEvents);
        Assert.Equal("/tmp/tsw", options.DefaultDownloadPath);
        Assert.Equal(TimeSpan.FromSeconds(5), options.DefaultTimeout);
        var rule = Assert.Single(options.IpFilterRules!);
        Assert.True(rule.Blocked);
    }

    [Fact]
    public void AnEmptyIpFilter_StillReplacesTheFilter()
    {
        var options = new TorrentClientOptionsBuilder();
        options.IpFilter(_ => { });

        Assert.NotNull(options.IpFilterRules);
        Assert.Empty(options.IpFilterRules);
    }

    [Fact]
    public void Advanced_RunsInOrder_SoItCanOverrideAnEarlierMethod()
    {
        var pack = Configure(o => o
            .Limits(l => l.Connections(10))
            .Advanced(p => p.Set(new ConnectionsLimit(20))));

        Assert.Equal(20, pack.Get<ConnectionsLimit>()?.Value);
    }

    [Fact]
    public void StartupOnlyOptions_AreRecorded()
    {
        var options = new TorrentClientOptionsBuilder();
        EventHandler<Notifications.SessionNotification> handler = (_, _) => { };
        options.OnNotification(handler).State([1, 2, 3]);

        Assert.Same(handler, Assert.Single(options.Handlers));
        Assert.Equal([1, 2, 3], options.SessionState);
    }

    [Theory]
    [InlineData("0.0.0.0:6881", "0.0.0.0:6881")]
    [InlineData("[::]:6881", "[::]:6881")]
    [InlineData("127.0.0.1:6881s", "127.0.0.1:6881s")]
    [InlineData("[::1]:6881l", "[::1]:6881l")]
    [InlineData("{6B29FC40-CA47-1067-B31D-00DD010662DA}", "{6B29FC40-CA47-1067-B31D-00DD010662DA}")]
    public void ListenInterface_ParsesLibtorrentsNotation(string text, string written)
    {
        Assert.Equal(written, ListenInterface.Parse(text).ToString());
    }

    [Theory]
    [InlineData("0.0.0.0")]
    [InlineData("::1")]
    [InlineData("eth0")]
    public void ListenInterface_RejectsWhatLibtorrentWouldNotTake(string text)
    {
        Assert.Throws<FormatException>(() => ListenInterface.Parse(text));
    }

    private static SettingsPack Configure(Action<ITorrentClientStartupOptions> configure)
    {
        var options = new TorrentClientOptionsBuilder();
        configure(options);
        return options.Pack;
    }
}
