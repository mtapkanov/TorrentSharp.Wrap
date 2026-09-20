using TorrentSharp.Wrap.Notifications;
using JetBrains.Annotations;
using TorrentSharp.Wrap.Configurations;
using Xunit.Abstractions;

namespace TorrentSharp.Wrap.Tests;

[TestSubject(typeof(TorrentManager))]
public partial class PieceStreamingTests : IDisposable
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
