using TorrentSharp.Wrap.Enums;
using JetBrains.Annotations;
using TorrentSharp.Wrap.Configurations;
using TorrentSharp.Wrap.Configurations.Settings;

namespace TorrentSharp.Wrap.Tests;

[TestSubject(typeof(TorrentClientConfig))]
public class TorrentClientConfigTests
{
    [Fact]
    public void Build_OmitsUserAgentAndFingerprint_WhenNotSet()
    {
        var pack = new TorrentClientConfig().Build();

        Assert.Null(pack.Get<UserAgent>());
        Assert.Null(pack.Get<PeerFingerprint>());
    }

    [Fact]
    public void Build_SetsUserAgentAndFingerprint_WhenProvided()
    {
        var pack = new TorrentClientConfig
        {
            UserAgent = "tsw/1.0",
            Fingerprint = "-TS0100-"
        }.Build();

        Assert.Equal("tsw/1.0", pack.Get<UserAgent>()?.Value);
        Assert.Equal("-TS0100-", pack.Get<PeerFingerprint>()?.Value);
    }

    [Fact]
    public void Build_SetsAlertMaskFromNotificationCategories()
    {
        var pack = new TorrentClientConfig
        {
            NotificationCategories = NotificationCategories.Status | NotificationCategories.Peer
        }.Build();

        Assert.Equal(NotificationCategories.Status | NotificationCategories.Peer, pack.Get<AlertMask>()?.Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Build_MapsPrivateModeToAnonymousMode(bool privateMode)
    {
        var pack = new TorrentClientConfig { PrivateMode = privateMode }.Build();

        Assert.Equal(privateMode, pack.Get<AnonymousMode>()?.Value);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Build_MapsBlockSeedingToInvertedSeedingOutgoingConnections(bool blockSeeding, bool expectedSeedingOutgoing)
    {
        var pack = new TorrentClientConfig { BlockSeeding = blockSeeding }.Build();

        Assert.Equal(expectedSeedingOutgoing, pack.Get<SeedingOutgoingConnections>()?.Value);
    }

    [Fact]
    public void Build_SetsConnectionsLimit_WhenMaxConnectionsIsSet()
    {
        var pack = new TorrentClientConfig { MaxConnections = 50 }.Build();

        Assert.Equal(50, pack.Get<ConnectionsLimit>()?.Value);
    }

    [Fact]
    public void Build_SetsEncryptionPolicy_WhenForceEncryptionIsTrue()
    {
        var pack = new TorrentClientConfig { ForceEncryption = true }.Build();

        Assert.Equal(0, pack.Get<OutEncPolicy>()?.Value);
        Assert.Equal(0, pack.Get<InEncPolicy>()?.Value);
    }
}
