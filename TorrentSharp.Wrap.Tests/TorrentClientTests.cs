using TorrentSharp.Wrap.Notifications;
using TorrentSharp.Wrap.Enums;
using JetBrains.Annotations;
using TorrentSharp.Wrap.Configurations;
using Xunit.Abstractions;

namespace TorrentSharp.Wrap.Tests;

[TestSubject(typeof(TorrentClient))]
public class TorrentClientTests : IDisposable
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
    public async Task TestTorrentDownload()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        var tcs = new TaskCompletionSource();

        // качаем только не-видео файлы (< 10мб)
        foreach (var file in torrentManager.Files.Where(x => x.Info.FileSize > 1e+7))
        {
            file.Priority = FileDownloadPriority.DoNotDownload;
        }

        try
        {
            torrentManager.Start();

            await using (new Timer(CheckProgress, (torrentManager, tcs), TimeSpan.Zero, TimeSpan.FromSeconds(5)))
            {
                await tcs.Task.WaitAsync(TimeSpan.FromMinutes(2));
            }

            // делаем повторный announce
            torrentManager.ReannounceAllTrackers(TimeSpan.Zero);

            // проверяем, что все файлы скачаны и имеют правильный размер
            foreach (var file in torrentManager.Files.Where(x => x.Priority != FileDownloadPriority.DoNotDownload))
            {
                Assert.True(File.Exists(file.Path));
                Assert.Equal(file.Info.FileSize, new FileInfo(file.Path).Length);
            }
        }
        finally
        {
            await PerformCleanup(torrentManager);
        }

        Assert.True(!_client.ActiveTorrents.Contains(torrentManager));
    }

    private void CheckProgress(object? state)
    {
        var (manager, tcs) = (ValueTuple<TorrentManager, TaskCompletionSource>)state!;
        var status = manager.GetCurrentStatus();

        if (status.State is TorrentState.Finished or TorrentState.Seeding)
        {
            tcs.TrySetResult();
        }

        _output.WriteLine($"Progress: {status.State} {status.Progress * 100:F2}% ({status.SeedCount:N0} seeds)");
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