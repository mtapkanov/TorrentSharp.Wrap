using TorrentSharp.Wrap.Notifications;
using TorrentSharp.Wrap.Enums;
using JetBrains.Annotations;
using TorrentSharp.Wrap.Configurations;
using Xunit.Abstractions;

namespace TorrentSharp.Wrap.Tests;

[TestSubject(typeof(TorrentManager))]
public class TorrentManagerTests : IDisposable
{
    private readonly TorrentClient _client = new(new TorrentClientConfig
    {
        ForceEncryption = true,
        BlockSeeding = true
    });

    private readonly ITestOutputHelper _output;
    private readonly string _tempSavePath;

    public TorrentManagerTests(ITestOutputHelper output)
    {
        _output = output;
        _tempSavePath = Path.Combine(Path.GetTempPath(), "tsw-manager-test");

        Directory.CreateDirectory(_tempSavePath);
    }

    public void Dispose()
    {
        _client?.Dispose();
        Directory.Delete(_tempSavePath, true);
    }

    [Fact]
    public async Task GetTrackers_ReturnsAnnounceListFromMetadata()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        try
        {
            // статический announce-list из .torrent-файла доступен сразу - никакого
            // announce для этого выполнять не нужно.
            var trackers = torrentManager.GetTrackers();

            Assert.NotEmpty(trackers);
            Assert.All(trackers, tracker => Assert.False(string.IsNullOrEmpty(tracker.Url)));
        }
        finally
        {
            await PerformCleanup(torrentManager);
        }
    }

    [Fact]
    public async Task GetPeers_ReturnsEmpty_BeforeAnyConnectionsAreMade()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        try
        {
            // торрент ещё не запущен, так что libtorrent физически не мог ни к кому подключиться
            Assert.Empty(torrentManager.GetPeers());
        }
        finally
        {
            await PerformCleanup(torrentManager);
        }
    }

    [Fact]
    public async Task GetPeersAndGetTrackers_ThrowAfterDetach()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        await PerformCleanup(torrentManager);

        Assert.Throws<ObjectDisposedException>(torrentManager.GetPeers);
        Assert.Throws<ObjectDisposedException>(torrentManager.GetTrackers);
    }

    [Fact]
    public async Task ReannounceAllTrackers_NegativeInterval_ThrowsArgumentOutOfRangeException()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        try
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => torrentManager.ReannounceAllTrackers(TimeSpan.FromSeconds(-1)));
        }
        finally
        {
            await PerformCleanup(torrentManager);
        }
    }

    [Fact]
    public async Task GetCurrentStatus_ReturnsUnstartedDefaults_BeforeStart()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        try
        {
            // торренты присоединяются в состоянии паузы (см. attach_torrent в library.cpp), но
            // state_t в libtorrent никак не связан с этим флагом - он отражает фазу, в которой
            // торрент был бы, если его возобновить, а не то, идёт ли передача реально. Progress и
            // PeerCount - единственные поля, на начальные значения которых тут можно положиться.
            var status = torrentManager.GetCurrentStatus();

            Assert.Equal(0, status.Progress);
            Assert.Equal(0, status.PeerCount);
        }
        finally
        {
            await PerformCleanup(torrentManager);
        }
    }

    [Fact]
    public async Task TorrentManagerFile_Priority_EventuallyReflectsSetValue()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        try
        {
            var file = torrentManager.Files[0];

            // torrent_handle::file_priority(index, priority) отправляет задачу во внутренний
            // поток сессии libtorrent, а не применяет значение синхронно, поэтому геттеру
            // нужен опрос, а не мгновенное чтение значения обратно.
            file.Priority = FileDownloadPriority.DoNotDownload;
            await AssertEventually(() => file.Priority == FileDownloadPriority.DoNotDownload);

            file.Priority = FileDownloadPriority.High;
            await AssertEventually(() => file.Priority == FileDownloadPriority.High);
        }
        finally
        {
            await PerformCleanup(torrentManager);
        }

        return;

        static async Task AssertEventually(Func<bool> condition)
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            while (!condition())
            {
                timeoutCts.Token.ThrowIfCancellationRequested();
                await Task.Delay(50, timeoutCts.Token);
            }
        }
    }

    [Fact]
    public async Task GetPeersAndGetTrackers_ReflectRealSwarmActivityOnceStarted()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        // качаем только не-видео файлы (< 10мб) - нас интересует активность роя, а не сама передача
        foreach (var file in torrentManager.Files.Where(x => x.Info.FileSize > 1e+7))
        {
            file.Priority = FileDownloadPriority.DoNotDownload;
        }

        var tcs = new TaskCompletionSource();

        try
        {
            torrentManager.Start();
            torrentManager.ReannounceAllTrackers(TimeSpan.Zero);

            await using (new Timer(CheckActivity, (torrentManager, tcs), TimeSpan.Zero, TimeSpan.FromSeconds(2)))
            {
                await tcs.Task.WaitAsync(TimeSpan.FromSeconds(60));
            }
        }
        finally
        {
            await PerformCleanup(torrentManager);
        }

        return;

        void CheckActivity(object? state)
        {
            var (manager, completion) = (ValueTuple<TorrentManager, TaskCompletionSource>)state!;
            var trackers = manager.GetTrackers();
            var peers = manager.GetPeers();

            _output.WriteLine($"Trackers verified: {trackers.Count(t => t.Verified)}/{trackers.Count}, peers connected: {peers.Count}");

            if (trackers.Any(t => t.Verified) || peers.Count > 0)
            {
                completion.TrySetResult();
            }
        }
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

        void CheckNotification(object? sender, SessionNotification notification)
        {
            if (notification is not TorrentRemovedNotification removed || !ReferenceEquals(removed.TorrentManager, manager))
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
