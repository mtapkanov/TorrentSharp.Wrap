using TorrentSharp.Wrap.Enums;

namespace TorrentSharp.Wrap.Tests;

// Тесты, требующие настоящего исходящего BitTorrent-трафика ([RequiresNetworkFact]), намеренно
// вынесены в отдельный файл - см. RequiresNetworkFactAttribute.cs.
public partial class TorrentClientTests
{
    [RequiresNetworkFact(enabled: false)]
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
}
