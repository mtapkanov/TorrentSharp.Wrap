using TorrentSharp.Wrap.Notifications;
using TorrentSharp.Wrap.Enums;
using JetBrains.Annotations;
using TorrentSharp.Wrap.Configurations;
using Xunit.Abstractions;

namespace TorrentSharp.Wrap.Tests;

[TestSubject(typeof(TorrentManager))]
public class PieceStreamingTests : IDisposable
{
    private readonly TorrentClient _client = new(new TorrentClientConfig
    {
        ForceEncryption = true,
        BlockSeeding = true
    });

    private readonly ITestOutputHelper _output;
    private readonly string _tempSavePath;

    public PieceStreamingTests(ITestOutputHelper output)
    {
        _output = output;
        _tempSavePath = Path.Combine(Path.GetTempPath(), "tsw-piece-test");

        Directory.CreateDirectory(_tempSavePath);
    }

    public void Dispose()
    {
        _client?.Dispose();
        Directory.Delete(_tempSavePath, true);
    }

    [Fact]
    public async Task ReadPieceAsync_ReturnsRealDataForADownloadedPiece()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        // качаем только самый маленький файл, чтобы тест выполнялся быстро
        var targetFile = torrentManager.Files.OrderBy(x => x.Info.FileSize).First();

        foreach (var file in torrentManager.Files.Where(x => x != targetFile))
        {
            file.Priority = FileDownloadPriority.DoNotDownload;
        }

        var pieceRequest = torrentManager.MapFileRange(targetFile.Info.Index, offset: 0, size: 1);
        Assert.True(pieceRequest.Piece >= 0, "Expected a valid piece mapping for the target file.");

        try
        {
            torrentManager.Start();

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var pieceData = await torrentManager.ReadPieceAsync(pieceRequest.Piece, timeoutCts.Token);

            Assert.NotEmpty(pieceData);
            Assert.True(torrentManager.HavePiece(pieceRequest.Piece));

            var pieceMap = torrentManager.GetPieceMap();
            Assert.Equal(1, pieceMap[pieceRequest.Piece]);

            _output.WriteLine($"Read piece {pieceRequest.Piece}: {pieceData.Length:N0} bytes");
        }
        finally
        {
            await PerformCleanup(torrentManager);
        }
    }

    [Fact]
    public async Task OpenFileStream_ReadsFullFileContentMatchingDiskCopy()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        // качаем только самый маленький файл, чтобы тест выполнялся быстро
        var targetFile = torrentManager.Files.OrderBy(x => x.Info.FileSize).First();

        foreach (var file in torrentManager.Files.Where(x => x != targetFile))
        {
            file.Priority = FileDownloadPriority.DoNotDownload;
        }

        try
        {
            torrentManager.Start();

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            await using var stream = torrentManager.OpenFileStream(targetFile.Info.Index);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, timeoutCts.Token);

            Assert.Equal(targetFile.Info.FileSize, memoryStream.Length);

            // после полного чтения через стрим файл на диске должен содержать те же байты -
            // читаем его напрямую, а не через libtorrent, чтобы проверка была независимой
            using var diskTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (!File.Exists(targetFile.Path))
            {
                diskTimeoutCts.Token.ThrowIfCancellationRequested();
                await Task.Delay(50, diskTimeoutCts.Token);
            }

            var diskBytes = await File.ReadAllBytesAsync(targetFile.Path, timeoutCts.Token);
            Assert.Equal(diskBytes, memoryStream.ToArray());
        }
        finally
        {
            await PerformCleanup(torrentManager);
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
                    _output.WriteLine($"Failed to delete file {file.Path}. Error: {ex.Message}");
                }
            }

            cleanupTask.TrySetResult();
        }
    }
}
