using TorrentSharp.Wrap.Notifications;
using JetBrains.Annotations;
using TorrentSharp.Wrap.Configurations;
using Xunit.Abstractions;

namespace TorrentSharp.Wrap.Tests;

[TestSubject(typeof(TorrentClient))]
public partial class TorrentClientTests : IDisposable
{
    private readonly TorrentClient _client = new(new TorrentClientConfig
    {
        ForceEncryption = true,
        BlockSeeding = true
    });

    private readonly ITestOutputHelper _output;
    private readonly string _tempSavePath;

    public TorrentClientTests(ITestOutputHelper output)
    {
        _output = output;
        _tempSavePath = Path.Combine(Path.GetTempPath(), "tsw-test");

        Directory.CreateDirectory(_tempSavePath);
    }

    public void Dispose()
    {
        _client?.Dispose();
        Directory.Delete(_tempSavePath, true);
    }

    [Fact]
    public void Dispose_CanBeCalledMoreThanOnce()
    {
        var client = new TorrentClient();

        client.Dispose();
        var exception = Record.Exception(() => client.Dispose());

        Assert.Null(exception);
    }

    [Fact]
    public void AttachTorrent_AfterDispose_ThrowsObjectDisposedException()
    {
        var client = new TorrentClient();
        client.Dispose();

        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));

        Assert.Throws<ObjectDisposedException>(() => client.AttachTorrent(torrentInfo, _tempSavePath));
    }

    [Fact]
    public async Task AttachTorrent_SameTorrentTwice_ThrowsInvalidOperationException()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        try
        {
            Assert.Throws<InvalidOperationException>(() => _client.AttachTorrent(torrentInfo, _tempSavePath));
        }
        finally
        {
            await PerformCleanup(torrentManager);
        }
    }

    [Fact]
    public void IsDhtRunning_DoesNotThrow()
    {
        _ = _client.IsDhtRunning;
    }

    [Fact]
    public void AddDhtNode_DoesNotThrow()
    {
        _client.AddDhtNode("router.bittorrent.com", 6881);
    }

    [Fact]
    public void AddIpFilterRuleAndClearIpFilter_DoNotThrow()
    {
        _client.AddIpFilterRule("1.2.3.4", "1.2.3.4", blocked: true);
        _client.ClearIpFilter();
    }

    [Fact]
    public async Task GetSessionStatsAsync_ReturnsNonEmptyMetricSet()
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var metrics = await _client.GetSessionStatsAsync(timeoutCts.Token);

        Assert.NotEmpty(metrics);
        Assert.Contains(metrics.Keys, key => key.StartsWith("net.", StringComparison.Ordinal));
    }

    private async Task PerformCleanup(TorrentManager manager)
    {
        var cleanupTask = new TaskCompletionSource();

        _client.NotificationRaised += CheckNotification;

        try
        {
            _client.DetachTorrent(manager);
            await cleanupTask.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }
        catch
        {
            // логируем предупреждение, но не роняем тест
            _output.WriteLine("Failed to cleanup torrent manager in time. The event may not have been raised.");
        }
        finally
        {
            _client.NotificationRaised -= CheckNotification;
        }

        return;

        void CheckNotification(object? sender, SessionNotification alert)
        {
            if (alert is not TorrentRemovedNotification removedNotification || !ReferenceEquals(removedNotification.TorrentManager, manager))
            {
                return;
            }

            foreach (var file in manager.Files)
            {
                try
                {
                    File.Delete(file.Path);
                }
                catch (Exception ex)
                {
                    // логируем ошибку, но не роняем тест
                    _output.WriteLine($"Failed to delete file {file.Path}. Error: {ex.Message}");
                }
            }

            cleanupTask.TrySetResult();
        }
    }
}