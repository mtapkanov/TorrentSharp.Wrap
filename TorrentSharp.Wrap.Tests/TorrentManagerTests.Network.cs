namespace TorrentSharp.Wrap.Tests;

// Тесты, требующие настоящего исходящего BitTorrent-трафика ([RequiresNetworkFact]), намеренно
// вынесены в отдельный файл - см. RequiresNetworkFactAttribute.cs.
public partial class TorrentManagerTests
{
    [RequiresNetworkFact(enabled: false)]
    public async Task ScrapeTrackerAsync_ReturnsPeerCounts()
    {
        var torrentInfo = new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent")));
        var torrentManager = _client.AttachTorrent(torrentInfo, _tempSavePath);

        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var (incomplete, complete) = await torrentManager.ScrapeTrackerAsync(0, timeoutCts.Token);

            Assert.True(incomplete >= 0);
            Assert.True(complete >= 0);
        }
        finally
        {
            await PerformCleanup(torrentManager);
        }
    }
}
