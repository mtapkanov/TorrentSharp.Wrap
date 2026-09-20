using TorrentSharp.Wrap.Enums;

namespace TorrentSharp.Wrap.Tests;

// Тесты, требующие настоящего исходящего BitTorrent-трафика ([RequiresNetworkFact]), намеренно
// вынесены в отдельный файл - см. RequiresNetworkFactAttribute.cs.
public partial class PieceStreamingTests
{
    [RequiresNetworkFact(enabled: false)]
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

    [RequiresNetworkFact(enabled: false)]
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

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

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
}
