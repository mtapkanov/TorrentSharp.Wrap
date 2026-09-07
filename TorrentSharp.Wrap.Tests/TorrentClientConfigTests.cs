using TorrentSharp.Wrap.Enums;
using JetBrains.Annotations;
using TorrentSharp.Wrap.Configurations;

namespace TorrentSharp.Wrap.Tests;

[TestSubject(typeof(TorrentClientConfig))]
public class TorrentClientConfigTests
{
    [Fact]
    public void Build_OmitsUserAgentAndFingerprint_WhenNotSet()
    {
        var pack = new TorrentClientConfig().Build();

        Assert.Null(pack.Get<string>("user_agent"));
        Assert.Null(pack.Get<string>("peer_fingerprint"));
    }

    [Fact]
    public void Build_SetsUserAgentAndFingerprint_WhenProvided()
    {
        var pack = new TorrentClientConfig
        {
            UserAgent = "tsw/1.0",
            Fingerprint = "-TS0100-"
        }.Build();

        Assert.Equal("tsw/1.0", pack.Get<string>("user_agent"));
        Assert.Equal("-TS0100-", pack.Get<string>("peer_fingerprint"));
    }

    [Fact]
    public void Build_SetsAlertMaskFromNotificationCategories()
    {
        var pack = new TorrentClientConfig
        {
            NotificationCategories = NotificationCategories.Status | NotificationCategories.Peer
        }.Build();

        Assert.Equal((int)(NotificationCategories.Status | NotificationCategories.Peer), pack.Get<int>("alert_mask"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Build_MapsPrivateModeToAnonymousMode(bool privateMode)
    {
        var pack = new TorrentClientConfig { PrivateMode = privateMode }.Build();

        Assert.Equal(privateMode, pack.Get<bool>("anonymous_mode"));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Build_MapsBlockSeedingToInvertedSeedingOutgoingConnections(bool blockSeeding, bool expectedSeedingOutgoing)
    {
        var pack = new TorrentClientConfig { BlockSeeding = blockSeeding }.Build();

        Assert.Equal(expectedSeedingOutgoing, pack.Get<bool>("seeding_outgoing_connections"));
    }

    [Fact]
    public void Build_SetsConnectionsLimit_WhenMaxConnectionsIsSet()
    {
        var pack = new TorrentClientConfig { MaxConnections = 50 }.Build();

        Assert.Equal(50, pack.Get<int>("connections_limit"));
    }

    [Fact]
    public void Build_SetsEncryptionPolicy_WhenForceEncryptionIsTrue()
    {
        var pack = new TorrentClientConfig { ForceEncryption = true }.Build();

        Assert.Equal(0, pack.Get<int>("out_enc_policy"));
        Assert.Equal(0, pack.Get<int>("in_enc_policy"));
    }
}
