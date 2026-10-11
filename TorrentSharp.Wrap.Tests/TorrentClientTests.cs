using TorrentSharp.Wrap.Notifications;
using JetBrains.Annotations;
using TorrentSharp.Wrap.Configurations;
using TorrentSharp.Wrap.Enums;
using TorrentSharp.Wrap.Utils;
using TorrentSharp.Wrap.Configurations.Settings;
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
    public async Task ReconfiguringListenInterface_FiresListenSucceededNotification()
    {
        // The listen alerts fire the instant the sockets open. A client created with categories already
        // on opens its default sockets inside the constructor, before any NotificationRaised subscriber
        // can exist, so those alerts are simply gone - subscribing afterwards and waiting would be a
        // race. Create it quiet instead (libtorrent's own default alert_mask is just `error`, and the
        // library no longer adds any), subscribe, then turn the categories on together with a new listen
        // interface so the sockets reopen with the subscriber already in place.
        using var client = new TorrentClient(new TorrentClientConfig());
        var notificationTcs = new TaskCompletionSource<SessionNotification>(TaskCreationOptions.RunContinuationsAsynchronously);

        client.NotificationRaised += (_, notification) =>
        {
            if (notification is ListenSucceededNotification or ListenFailedNotification)
                notificationTcs.TrySetResult(notification);
        };

        var probe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        probe.Start();
        var port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        client.UpdateSettings(new SettingsPack()
            .Set(new AlertMask(NotificationCategories.Status))
            .SetListenInterfaces(new IpInterface(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port))));

        var notification = await notificationTcs.Task.WaitAsync(TimeSpan.FromSeconds(30));

        var succeeded = Assert.IsType<ListenSucceededNotification>(notification);
        Assert.Equal(port, succeeded.Port);
        Assert.Equal(System.Net.IPAddress.Loopback.ToString(), succeeded.Address);
    }

    [Fact]
    public async Task SeveralClientsInOneProcess_EachKeepsReceivingItsOwnNotifications()
    {
        // Alert delivery used to share one process-wide try_lock between every session: while one
        // session's alerts were being handled, another's wake-up was dropped - and since libtorrent
        // only wakes the host when its queue goes from empty to non-empty, a dropped wake-up silenced
        // that session for good. Several clients each reconfigured at the same moment is exactly the
        // interleaving that triggered it.
        const int clientCount = 6;
        var clients = Enumerable.Range(0, clientCount).Select(_ => new TorrentClient(new TorrentClientConfig())).ToArray();

        try
        {
            var received = clients
                .Select(client =>
                {
                    var tcs = new TaskCompletionSource<ListenSucceededNotification>(TaskCreationOptions.RunContinuationsAsynchronously);
                    client.NotificationRaised += (_, notification) =>
                    {
                        if (notification is ListenSucceededNotification succeeded)
                            tcs.TrySetResult(succeeded);
                    };
                    return tcs;
                })
                .ToArray();

            var ports = clients.Select(_ => FreeLoopbackPort()).ToArray();

            for (var i = 0; i < clientCount; i++)
            {
                clients[i].UpdateSettings(new SettingsPack()
                    .Set(new AlertMask(NotificationCategories.Status))
                    .SetListenInterfaces(new IpInterface(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, ports[i]))));
            }

            var notifications = await Task.WhenAll(received.Select(tcs => tcs.Task)).WaitAsync(TimeSpan.FromSeconds(30));

            Assert.Equal(ports.OrderBy(port => port), notifications.Select(n => n.Port).OrderBy(port => port));
        }
        finally
        {
            foreach (var client in clients)
                client.Dispose();
        }
    }

    private static int FreeLoopbackPort()
    {
        var probe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        probe.Start();
        var port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
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