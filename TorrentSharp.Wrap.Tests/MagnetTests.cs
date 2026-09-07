using TorrentSharp.Wrap.Notifications;
using JetBrains.Annotations;
using TorrentSharp.Wrap.Configurations;
using Xunit.Abstractions;

namespace TorrentSharp.Wrap.Tests;

[TestSubject(typeof(TorrentClient))]
public class MagnetTests : IDisposable
{
    private readonly TorrentClient _client = new(new TorrentClientConfig
    {
        ForceEncryption = true,
        BlockSeeding = true
    });

    private readonly ITestOutputHelper _output;
    private readonly string _tempSavePath;

    // значения, выведенные из фикстуры, чтобы тесты оставались согласованы с .torrent-файлом на диске
    private readonly string _bigBuckBunnyMagnet;
    private readonly string _bigBuckBunnyInfoHash;
    private readonly string _bigBuckBunnyName;
    private readonly int _bigBuckBunnyFileCount;
    private readonly long _bigBuckBunnyTotalSize;

    public MagnetTests(ITestOutputHelper output)
    {
        _output = output;
        _tempSavePath = Path.Combine(Path.GetTempPath(), "tsw-magnet-test");
        Directory.CreateDirectory(_tempSavePath);

        var fixture = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        _bigBuckBunnyInfoHash = fixture.Metadata.InfoHash ?? throw new InvalidOperationException("Fixture torrent has no v1 info-hash.");
        _bigBuckBunnyMagnet = $"magnet:?xt=urn:btih:{_bigBuckBunnyInfoHash}&dn=Big+Buck+Bunny";
        _bigBuckBunnyName = fixture.Metadata.Name;
        _bigBuckBunnyFileCount = fixture.Metadata.TotalFiles;
        _bigBuckBunnyTotalSize = fixture.Metadata.TotalSize;
    }

    public void Dispose()
    {
        _client?.Dispose();
        Directory.Delete(_tempSavePath, true);
    }

    [Fact]
    public void AttachMagnet_InvalidUri_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => _client.AttachMagnet("not-a-magnet-uri", _tempSavePath));
    }

    [Fact]
    public void AttachMagnet_ValidUri_ReturnsManagerWithMatchingInfoHash()
    {
        var manager = _client.AttachMagnet(_bigBuckBunnyMagnet, _tempSavePath);

        Assert.Equal(_bigBuckBunnyInfoHash, manager.InfoHash, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void AttachMagnet_InfoIsNullBeforeMetadataFetched()
    {
        var manager = _client.AttachMagnet(_bigBuckBunnyMagnet, _tempSavePath);

        Assert.Null(manager.Info);
    }

    [Fact]
    public void AttachMagnet_FilesIsEmptyBeforeMetadataFetched()
    {
        var manager = _client.AttachMagnet(_bigBuckBunnyMagnet, _tempSavePath);

        Assert.Empty(manager.Files);
    }

    [Fact]
    public void AttachMagnet_DuplicateUri_ThrowsInvalidOperationException()
    {
        _client.AttachMagnet(_bigBuckBunnyMagnet, _tempSavePath);

        Assert.Throws<InvalidOperationException>(() => _client.AttachMagnet(_bigBuckBunnyMagnet, _tempSavePath));
    }

    [Fact]
    public void AttachMagnet_AppearsInActiveTorrents()
    {
        var manager = _client.AttachMagnet(_bigBuckBunnyMagnet, _tempSavePath);

        Assert.Contains(manager, _client.ActiveTorrents);
    }

    [Fact]
    public async Task DetachMagnet_BeforeMetadata_RemovesFromActiveTorrents()
    {
        var manager = _client.AttachMagnet(_bigBuckBunnyMagnet, _tempSavePath);
        var removedTcs = new TaskCompletionSource();

        _client.NotificationRaised += (_, alert) =>
        {
            if (alert is TorrentRemovedNotification removed && ReferenceEquals(removed.TorrentManager, manager))
            {
                removedTcs.TrySetResult();
            }
        };

        _client.DetachTorrent(manager);
        await removedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.DoesNotContain(manager, _client.ActiveTorrents);
    }

    [Fact]
    public async Task TestMagnetMetadataFetch()
    {
        var manager = _client.AttachMagnet(_bigBuckBunnyMagnet, _tempSavePath);

        Assert.Null(manager.Info);
        Assert.Empty(manager.Files);

        manager.Start();

        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await manager.WaitForMetadata(cancellationTokenSource.Token);

        Assert.NotNull(manager.Info);
        Assert.NotEmpty(manager.Files);
        Assert.Equal(_bigBuckBunnyName, manager.Info.Metadata.Name);
        Assert.Equal(_bigBuckBunnyFileCount, manager.Info.Metadata.TotalFiles);
        Assert.Equal(_bigBuckBunnyTotalSize, manager.Info.Metadata.TotalSize);

        await CleanupAsync(manager);
    }

    [Fact]
    public async Task TestMagnetSaveTorrentFile()
    {
        var manager = _client.AttachMagnet(_bigBuckBunnyMagnet, _tempSavePath);
        var savedPath = Path.Combine(_tempSavePath, "saved.torrent");

        manager.Start();

        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await manager.WaitForMetadata(cancellationTokenSource.Token);

        manager.Info?.SaveToFile(savedPath);
        Assert.True(File.Exists(savedPath));

        var loaded = new TorrentInfo(savedPath);
        Assert.Equal(_bigBuckBunnyName, loaded.Metadata.Name);
        Assert.Equal(_bigBuckBunnyFileCount, loaded.Metadata.TotalFiles);
        Assert.Equal(_bigBuckBunnyTotalSize, loaded.Metadata.TotalSize);

        await CleanupAsync(manager);
    }

    [Fact]
    public void SaveTorrentFile_ThrowsIfNoMetadata()
    {
        var manager = _client.AttachMagnet(_bigBuckBunnyMagnet, _tempSavePath);

        Assert.Throws<NullReferenceException>(() => manager.Info!.SaveToFile("ignored.torrent"));
    }

    private async Task CleanupAsync(TorrentManager manager)
    {
        var removedTcs = new TaskCompletionSource();

        _client.NotificationRaised += CheckNotification;

        try
        {
            _client.DetachTorrent(manager);
            await removedTcs.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }
        catch
        {
            _output.WriteLine("Cleanup timed out — torrent removed alert may not have fired.");
        }
        finally
        {
            _client.NotificationRaised -= CheckNotification;
        }

        return;

        void CheckNotification(object? _, SessionNotification alert)
        {
            if (alert is TorrentRemovedNotification removed && ReferenceEquals(removed.TorrentManager, manager))
            {
                removedTcs.TrySetResult();
            }
        }
    }
}
