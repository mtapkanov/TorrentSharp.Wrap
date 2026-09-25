using TorrentSharp.Wrap.Notifications;
using TorrentSharp.Wrap.Enums;
using JetBrains.Annotations;
using TorrentSharp.Wrap.Configurations;
using Xunit.Abstractions;

namespace TorrentSharp.Wrap.Tests;

[TestSubject(typeof(TorrentManager))]
public partial class TorrentManagerTests : IDisposable
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
    public void TorrentManagerMethods_ThrowObjectDisposedImmediatelyAfterDetach_WithoutWaitingForNotification()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        // TorrentClient.DetachTorrent удаляет менеджер из своего словаря только когда придёт
        // асинхронное уведомление TorrentRemoved, но нативный detach_torrent уже освободил
        // torrent_handle синхронно, к моменту как этот вызов вернул управление. До фикса это
        // оставляло окно, где _detached ещё false, а любой из вызовов ниже уходил в нативный код
        // с висячим указателем вместо ObjectDisposedException - специально не ждём уведомление
        // здесь, чтобы гарантированно попасть в это окно.
        _client.DetachTorrent(torrentManager);

        Assert.Throws<ObjectDisposedException>(torrentManager.GetPeers);
        Assert.Throws<ObjectDisposedException>(torrentManager.GetTrackers);
        Assert.Throws<ObjectDisposedException>(() => torrentManager.GetCurrentStatus());
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
    }

    [Fact]
    public async Task PiecePriority_EventuallyReflectsSetValue()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        try
        {
            // как и file_priority, приоритет куска применяется через внутренний поток сессии
            // libtorrent, поэтому геттеру нужен опрос.
            torrentManager.SetPiecePriority(0, FileDownloadPriority.DoNotDownload);
            await AssertEventually(() => torrentManager.GetPiecePriority(0) == FileDownloadPriority.DoNotDownload);

            torrentManager.SetPiecePriority(0, FileDownloadPriority.High);
            await AssertEventually(() => torrentManager.GetPiecePriority(0) == FileDownloadPriority.High);
        }
        finally
        {
            await PerformCleanup(torrentManager);
        }
    }

    [Fact]
    public async Task PiecePriorities_BulkSetEventuallyReflectsInBulkGet()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        try
        {
            var priorities = new FileDownloadPriority[torrentManager.PieceCount];
            Array.Fill(priorities, FileDownloadPriority.DoNotDownload);

            torrentManager.SetPiecePriorities(priorities);
            await AssertEventually(() => torrentManager.GetPiecePriorities().All(p => p == FileDownloadPriority.DoNotDownload));
        }
        finally
        {
            await PerformCleanup(torrentManager);
        }
    }

    [Fact]
    public async Task GetCurrentStatus_ReturnsUnstartedDefaultsForExtendedFields_BeforeStart()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        try
        {
            var status = torrentManager.GetCurrentStatus();

            Assert.Equal(0, status.TotalWantedDone);
            Assert.Equal(0, status.AllTimeUpload);
            Assert.Equal(0, status.AllTimeDownload);
            Assert.False(status.IsFinished);
            Assert.False(status.MovingStorage);
            Assert.True(status.TotalWanted > 0);
            Assert.True(status.AddedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
        }
        finally
        {
            await PerformCleanup(torrentManager);
        }
    }

    [Fact]
    public async Task UploadAndDownloadLimit_EventuallyReflectSetValue()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        try
        {
            // как и file_priority, лимиты применяются через внутренний поток сессии
            // libtorrent, поэтому геттеру нужен опрос.
            torrentManager.UploadLimit = 1024;
            await AssertEventually(() => torrentManager.UploadLimit == 1024);

            torrentManager.DownloadLimit = 2048;
            await AssertEventually(() => torrentManager.DownloadLimit == 2048);
        }
        finally
        {
            await PerformCleanup(torrentManager);
        }
    }

    [Fact]
    public async Task TorrentFlags_EventuallyReflectSetValue()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        try
        {
            torrentManager.SequentialDownload = true;
            await AssertEventually(() => torrentManager.SequentialDownload);

            torrentManager.ShareMode = true;
            await AssertEventually(() => torrentManager.ShareMode);

            torrentManager.UploadMode = true;
            await AssertEventually(() => torrentManager.UploadMode);
        }
        finally
        {
            await PerformCleanup(torrentManager);
        }
    }

    [Fact]
    public async Task QueuePosition_MovementMethods_DoNotThrow()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        try
        {
            _ = torrentManager.QueuePosition;

            torrentManager.MoveQueueToTop();
            torrentManager.MoveQueueUp();
            torrentManager.MoveQueueDown();
            torrentManager.MoveQueueToBottom();
        }
        finally
        {
            await PerformCleanup(torrentManager);
        }
    }

    [Fact]
    public async Task ForceRecheck_DoesNotThrow()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        try
        {
            torrentManager.ForceRecheck();
        }
        finally
        {
            await PerformCleanup(torrentManager);
        }
    }

    [Fact]
    public async Task RenameFileAsync_CompletesSuccessfully()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await torrentManager.RenameFileAsync(0, "renamed-file.bin", timeoutCts.Token);
        }
        finally
        {
            await PerformCleanup(torrentManager);
        }
    }

    [Fact]
    public async Task MoveStorageAsync_CompletesSuccessfully()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        var newPath = Path.Combine(_tempSavePath, "moved");
        Directory.CreateDirectory(newPath);

        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await torrentManager.MoveStorageAsync(newPath, timeoutCts.Token);
        }
        finally
        {
            await PerformCleanup(torrentManager);
        }
    }

    [Fact]
    public async Task AddTracker_EventuallyAppearsInGetTrackers()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        try
        {
            const string url = "udp://tracker.example.test:80/announce";

            // как и другие torrent_handle-методы, add_tracker отправляет задачу во внутренний
            // поток сессии libtorrent, поэтому геттеру нужен опрос.
            torrentManager.AddTracker(url, tier: 5);
            await AssertEventually(() => torrentManager.GetTrackers().Any(t => t.Url == url));
        }
        finally
        {
            await PerformCleanup(torrentManager);
        }
    }

    [Fact]
    public async Task ReplaceTrackers_EventuallyReplacesAnnounceList()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        try
        {
            const string url = "udp://tracker.example.test:80/announce";
            torrentManager.ReplaceTrackers([(url, (byte)0)]);

            await AssertEventually(() =>
            {
                var trackers = torrentManager.GetTrackers();
                return trackers.Count == 1 && trackers[0].Url == url;
            });
        }
        finally
        {
            await PerformCleanup(torrentManager);
        }
    }

    [Fact]
    public async Task AddUrlSeedAndAddHttpSeed_DoNotThrow()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        try
        {
            torrentManager.AddUrlSeed("http://example.test/seed/");
            torrentManager.AddHttpSeed("http://example.test/http-seed/");
        }
        finally
        {
            await PerformCleanup(torrentManager);
        }
    }

    [Fact]
    public async Task SaveResumeDataAsync_ReturnsNonEmptyBuffer()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var data = await torrentManager.SaveResumeDataAsync(timeoutCts.Token);

            Assert.NotEmpty(data);
        }
        finally
        {
            await PerformCleanup(torrentManager);
        }
    }

    [Fact]
    public async Task AttachTorrent_WithResumeData_RoundTripsSuccessfully()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var firstManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        byte[] resumeData;
        using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
        {
            resumeData = await firstManager.SaveResumeDataAsync(timeoutCts.Token);
        }

        await PerformCleanup(firstManager);

        var firstInfoHash = firstManager.InfoHash;
        var secondManager = _client.AttachTorrent(torrentInfo, _tempSavePath, resumeData);

        try
        {
            Assert.Equal(firstInfoHash, secondManager.InfoHash);
            Assert.NotEmpty(secondManager.Files);
        }
        finally
        {
            await PerformCleanup(secondManager);
        }
    }

    [Fact]
    public async Task ForceRecheck_FileNotReadable_RaisesFileErrorNotification()
    {
        // File.SetUnixFileMode below has no Windows equivalent - this library isn't built for
        // Windows anyway (see scripts/build.sh's presets), so there's nothing to exercise there.
        if (OperatingSystem.IsWindows())
            return;

        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        try
        {
            var file = torrentManager.Files[0];
            Directory.CreateDirectory(Path.GetDirectoryName(file.Path)!);
            // Sized to exactly match what the torrent expects - a size mismatch would let libtorrent
            // conclude "needs downloading" from stat() alone, without ever calling open() for a real
            // read and hitting the permission error this test is actually after.
            using (var placeholder = File.Create(file.Path))
                placeholder.SetLength(file.Info.FileSize);
            // No read permission at all - libtorrent's open() during the recheck below must fail
            // with a real OS error (EACCES), not just see "file doesn't exist yet" (the normal,
            // silent case for a torrent that's never been downloaded).
            File.SetUnixFileMode(file.Path, UnixFileMode.None);

            var errorTcs = new TaskCompletionSource<FileErrorNotification>(TaskCreationOptions.RunContinuationsAsynchronously);

            void OnNotification(object? sender, SessionNotification notification)
            {
                if (notification is FileErrorNotification fileError)
                    errorTcs.TrySetResult(fileError);
            }

            _client.NotificationRaised += OnNotification;
            try
            {
                // Torrents attach paused by default (see library.cpp's AttachTorrent) - libtorrent
                // doesn't actually run a forced recheck's disk I/O until the torrent is unpaused.
                torrentManager.Start();
                torrentManager.ForceRecheck();

                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await using (timeoutCts.Token.Register(() => errorTcs.TrySetCanceled()))
                {
                    var notification = await errorTcs.Task;

                    Assert.Equal(torrentManager.InfoHash, notification.TorrentManager.InfoHash);
                    Assert.NotEmpty(notification.Filename);
                }
            }
            finally
            {
                _client.NotificationRaised -= OnNotification;
            }
        }
        finally
        {
            await PerformCleanup(torrentManager);
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

    private static async Task AssertEventually(Func<bool> condition)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        while (!condition())
        {
            timeoutCts.Token.ThrowIfCancellationRequested();
            await Task.Delay(50, timeoutCts.Token);
        }
    }
}
