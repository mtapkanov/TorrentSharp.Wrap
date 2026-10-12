using System.Security.Cryptography;
using System.Text;
using JetBrains.Annotations;
using TorrentSharp.Wrap.Configurations;
using TorrentSharp.Wrap.Enums;
using TorrentSharp.Wrap.Notifications;

namespace TorrentSharp.Wrap.Tests;

/// <summary>
/// Saving resume data with options, turning it into a .torrent, and attaching with resume data that
/// carries the metadata or belongs to another torrent. Every torrent stays paused, so nothing here
/// touches the network.
/// </summary>
[TestSubject(typeof(TorrentManager))]
public class ResumeDataTests : IDisposable
{
    private const int PieceLength = 16 * 1024;

    private readonly TorrentClient _client = new(new TorrentClientConfig
    {
        BlockSeeding = true,
        // save_resume_data_alert is a Storage alert, torrent_removed_alert a Status one.
        NotificationCategories = NotificationCategories.Status | NotificationCategories.Storage
    });

    private readonly string _savePath = Directory.CreateTempSubdirectory("tsw-resume-test-").FullName;

    private static TorrentInfo BigBuckBunny() =>
        new(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));

    private static TorrentInfo Ubuntu() =>
        new(Path.GetFullPath(Path.Combine("files", "ubuntu-20.04.6-live-server-amd64.iso.torrent")));

    public void Dispose()
    {
        _client.Dispose();
        Directory.Delete(_savePath, true);
    }

    [Fact]
    public async Task IfModified_SkipsTheSave_OnlyWhileNothingHasChanged()
    {
        var manager = _client.AttachTorrent(BigBuckBunny(), _savePath);
        try
        {
            Assert.NotNull(await manager.SaveResumeDataAsync(ResumeDataFlags.None, CancellationToken.None));

            Assert.Null(await manager.SaveResumeDataAsync(ResumeDataFlags.IfModified, CancellationToken.None));

            // A new tracker is a metadata change, recorded before the save request is handled. (A file
            // priority would do too, but libtorrent records it only once a disk job comes back.)
            manager.AddTracker("udp://tracker.example.test:1337/announce");
            Assert.NotNull(await manager.SaveResumeDataAsync(ResumeDataFlags.IfModified, CancellationToken.None));
        }
        finally
        {
            await DetachAsync(manager);
        }
    }

    [Fact]
    public async Task IfModified_SkippedSave_IsReportedAsNotModified()
    {
        var manager = _client.AttachTorrent(BigBuckBunny(), _savePath);
        var notifications = new List<ResumeDataNotification>();
        _client.NotificationRaised += (_, notification) =>
        {
            if (notification is ResumeDataNotification resume)
                lock (notifications) notifications.Add(resume);
        };

        try
        {
            await manager.SaveResumeDataAsync(ResumeDataFlags.None, CancellationToken.None);
            await manager.SaveResumeDataAsync(ResumeDataFlags.IfModified, CancellationToken.None);

            lock (notifications)
            {
                Assert.Equal(2, notifications.Count);
                Assert.False(notifications[0].NotModified);
                Assert.True(notifications[1] is { Succeeded: false, NotModified: true });
            }
        }
        finally
        {
            await DetachAsync(manager);
        }
    }

    [Fact]
    public async Task TryCreateBytesFromResumeData_MakesTheSameTorrent_WithItsCurrentTrackersAndWebSeeds()
    {
        var original = BigBuckBunny();
        var manager = _client.AttachTorrent(original, _savePath);
        try
        {
            manager.AddTracker("udp://tracker.example.test:1337/announce");
            manager.AddUrlSeed("https://seed.example.test/");
            var resumeData = await manager.SaveResumeDataAsync(ResumeDataFlags.SaveInfoDict, CancellationToken.None);

            Assert.True(TorrentInfo.TryCreateBytesFromResumeData(resumeData!, out var torrentBytes));

            var converted = new TorrentInfo(torrentBytes);
            Assert.Equal(original.Metadata.InfoHash, converted.Metadata.InfoHash);
            Assert.Equal(original.Files.Select(file => file.Path), converted.Files.Select(file => file.Path));

            var text = Encoding.ASCII.GetString(torrentBytes);
            Assert.Contains("udp://tracker.opentrackr.org:1337", text);
            Assert.Contains("udp://tracker.example.test:1337/announce", text);
            Assert.Contains("https://webtorrent.io/torrents/", text);
            Assert.Contains("https://seed.example.test/", text);
        }
        finally
        {
            await DetachAsync(manager);
        }
    }

    [Fact]
    public async Task TryCreateBytesFromResumeData_WithoutTheInfoDict_ReturnsFalse()
    {
        var manager = _client.AttachTorrent(BigBuckBunny(), _savePath);
        try
        {
            var resumeData = await manager.SaveResumeDataAsync(ResumeDataFlags.None, CancellationToken.None);

            Assert.False(TorrentInfo.TryCreateBytesFromResumeData(resumeData!, out var torrentBytes));
            Assert.Null(torrentBytes);
            Assert.False(TorrentInfo.TryCreateBytesFromResumeData("not bencode"u8.ToArray(), out _));
        }
        finally
        {
            await DetachAsync(manager);
        }
    }

    [Fact]
    public async Task AHybridTorrentMissingItsPieceLayers_StillBecomesATorrentFile()
    {
        // What a v2/hybrid magnet's metadata looks like before the download finishes: the info dict, but
        // not the piece layers. libtorrent refuses to write such a .torrent unless told it may.
        var hybridBytes = BuildHybridTorrentWithoutPieceLayers("hybrid.bin", 40_000, seed: 77);
        var hybrid = new TorrentInfo(hybridBytes);

        Assert.NotEmpty(hybrid.GetBytes());

        var manager = _client.AttachTorrent(hybrid, _savePath);
        try
        {
            var resumeData = await manager.SaveResumeDataAsync(ResumeDataFlags.SaveInfoDict, CancellationToken.None);

            Assert.True(TorrentInfo.TryCreateBytesFromResumeData(resumeData!, out var torrentBytes));
            var converted = new TorrentInfo(torrentBytes);
            Assert.Equal(hybrid.Metadata.InfoHash, converted.Metadata.InfoHash);
            Assert.NotNull(converted.Metadata.InfoHashV2);
        }
        finally
        {
            await DetachAsync(manager);
        }
    }

    [Fact]
    public async Task AttachMagnet_WithResumeDataCarryingTheMetadata_HasItAtOnce()
    {
        var original = BigBuckBunny();
        var first = _client.AttachTorrent(original, _savePath);
        var resumeData = await first.SaveResumeDataAsync(ResumeDataFlags.SaveInfoDict, CancellationToken.None);
        await DetachAsync(first);

        var magnet = _client.AttachMagnet($"magnet:?xt=urn:btih:{original.Metadata.InfoHash}", _savePath, resumeData);
        try
        {
            // libtorrent raises no metadata_received_alert for metadata it had from the start.
            Assert.NotNull(magnet.Info);
            Assert.Equal(original.Metadata.Name, magnet.Info.Metadata.Name);
            await magnet.WaitForMetadata(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(1));
        }
        finally
        {
            await DetachAsync(magnet);
        }
    }

    [Fact]
    public async Task AttachWithAnotherTorrentsResumeData_AttachesTheRequestedTorrent()
    {
        var bigBuckBunny = _client.AttachTorrent(BigBuckBunny(), _savePath);
        var foreignResumeData = await bigBuckBunny.SaveResumeDataAsync(ResumeDataFlags.SaveInfoDict, CancellationToken.None);
        await DetachAsync(bigBuckBunny);

        var ubuntu = Ubuntu();
        var fromFile = _client.AttachTorrent(ubuntu, _savePath, foreignResumeData);
        try
        {
            Assert.Equal(ubuntu.Metadata.InfoHash, fromFile.InfoHash);
            Assert.Equal(ubuntu.Metadata.Name, fromFile.Info!.Metadata.Name);
        }
        finally
        {
            await DetachAsync(fromFile);
        }

        // For a magnet, resume data used to take over entirely - including which torrent it is.
        var fromMagnet = _client.AttachMagnet($"magnet:?xt=urn:btih:{ubuntu.Metadata.InfoHash}", _savePath, foreignResumeData);
        try
        {
            Assert.Equal(ubuntu.Metadata.InfoHash, fromMagnet.InfoHash);
            Assert.Null(fromMagnet.Info);
        }
        finally
        {
            await DetachAsync(fromMagnet);
        }
    }

    private async Task DetachAsync(TorrentManager manager)
    {
        var removed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnNotification(object? sender, SessionNotification notification)
        {
            if (notification is TorrentRemovedNotification removal && removal.TorrentManager == manager)
                removed.TrySetResult();
        }

        _client.NotificationRaised += OnNotification;
        try
        {
            _client.DetachTorrent(manager);
            await removed.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }
        finally
        {
            _client.NotificationRaised -= OnNotification;
        }
    }

    // Single-file hybrid (v1 + v2) torrent, with a correct v2 "pieces root" but no "piece layers".
    private static byte[] BuildHybridTorrentWithoutPieceLayers(string name, int length, int seed)
    {
        var content = new byte[length];
        new Random(seed).NextBytes(content);

        var v1Pieces = content.Chunk(PieceLength).SelectMany(SHA1.HashData).ToArray();
        var piecesRoot = MerkleRoot(content.Chunk(PieceLength).Select(SHA256.HashData).ToList());

        var fileEntry = Dict(("length", Int(length)), ("pieces root", Bytes(piecesRoot)));
        var info = Dict(
            ("file tree", Dict((name, Dict(("", fileEntry))))),
            ("length", Int(length)),
            ("meta version", Int(2)),
            ("name", Text(name)),
            ("piece length", Int(PieceLength)),
            ("pieces", Bytes(v1Pieces)));

        return Dict(("announce", Text("udp://127.0.0.1:1/announce")), ("info", info));
    }

    // BEP 52: leaves are the SHA-256 of each 16 KiB block, padded with zero hashes to a power of two.
    private static byte[] MerkleRoot(List<byte[]> leaves)
    {
        var count = 1;
        while (count < leaves.Count)
            count *= 2;
        while (leaves.Count < count)
            leaves.Add(new byte[32]);

        while (leaves.Count > 1)
            leaves = [.. leaves.Chunk(2).Select(pair => SHA256.HashData([.. pair[0], .. pair[1]]))];

        return leaves[0];
    }

    private static byte[] Int(long value) => Encoding.ASCII.GetBytes($"i{value}e");

    private static byte[] Bytes(byte[] value) => [.. Encoding.ASCII.GetBytes($"{value.Length}:"), .. value];

    private static byte[] Text(string value) => Bytes(Encoding.UTF8.GetBytes(value));

    // Keys must already be in sorted order, as bencode requires.
    private static byte[] Dict(params (string Key, byte[] Value)[] entries) =>
        [(byte)'d', .. entries.SelectMany(entry => (byte[])[.. Text(entry.Key), .. entry.Value]), (byte)'e'];
}
