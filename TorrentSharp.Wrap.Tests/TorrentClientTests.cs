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
    private readonly TorrentClient _client = new(options => options
        .Privacy(privacy => privacy.Encryption(EncryptionPolicy.Forced))
        .Limits(limits => limits.SeedingOutgoingConnections(false)));

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
        // Created quiet (libtorrent's own default alert_mask is just `error`), subscribed afterwards, then
        // the categories are turned on together with a new listen interface, so the sockets reopen with the
        // subscriber already in place. (Subscribing from the start is OnNotification - see the next test.)
        using var client = new TorrentClient();
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

        client.ChangeOptions(options => options
            .Notifications(notifications => notifications.Categories(NotificationCategories.Status))
            .Network(net => net.ListenOn(System.Net.IPAddress.Loopback, port)));

        var notification = await notificationTcs.Task.WaitAsync(TimeSpan.FromSeconds(30));

        var succeeded = Assert.IsType<ListenSucceededNotification>(notification);
        Assert.Equal(port, succeeded.Port);
        Assert.Equal(System.Net.IPAddress.Loopback.ToString(), succeeded.Address);
    }

    [Fact]
    public async Task CreatedListening_ReportsItsSocketToAHandlerSubscribedAtStartup()
    {
        // The listen alerts fire the instant the sockets open - inside the constructor for a client created
        // already listening. Only a handler subscribed through OnNotification is there in time.
        var port = FreeLoopbackPort();
        var notificationTcs = new TaskCompletionSource<SessionNotification>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var client = new TorrentClient(options => options
            .Notifications(notifications => notifications.Categories(NotificationCategories.Status))
            .Network(net => net.ListenOn(System.Net.IPAddress.Loopback, port))
            .OnNotification((_, notification) =>
            {
                if (notification is ListenSucceededNotification or ListenFailedNotification)
                    notificationTcs.TrySetResult(notification);
            }));

        var succeeded = Assert.IsType<ListenSucceededNotification>(await notificationTcs.Task.WaitAsync(TimeSpan.FromSeconds(30)));
        Assert.Equal(port, succeeded.Port);
        Assert.Equal(System.Net.IPAddress.Loopback.ToString(), succeeded.Address);
    }

    [Fact]
    public async Task ChangingOneSetting_LeavesTheOthersAsTheyWere()
    {
        // Only Status is enabled, then only the listen port changes: if ChangeOptions sent anything beyond
        // what it was given (a default alert_mask, say), the new socket's notification would never come.
        var firstPort = FreeLoopbackPort();
        var secondPort = FreeLoopbackPort();
        var listening = new System.Collections.Concurrent.ConcurrentQueue<int>();

        using var client = new TorrentClient(options => options
            .Notifications(notifications => notifications.Categories(NotificationCategories.Status))
            .Network(net => net.ListenOn(System.Net.IPAddress.Loopback, firstPort))
            .OnNotification((_, notification) =>
            {
                if (notification is ListenSucceededNotification succeeded)
                    listening.Enqueue(succeeded.Port);
            }));

        await WaitUntilAsync(() => listening.Contains(firstPort));
        client.ChangeOptions(options => options.Network(net => net.ListenOn(System.Net.IPAddress.Loopback, secondPort)));

        await WaitUntilAsync(() => listening.Contains(secondPort));
    }

    [Fact]
    public async Task DefaultTimeout_AppliesWhenACallPassesNone_AndAnExplicitOneWins()
    {
        // No category enables file_renamed_alert / file_rename_failed_alert, so a rename can only end by timing
        // out. (Not every async method would do: libtorrent posts some answers, such as resume data and
        // session stats, whatever the alert mask says.)
        using var client = new TorrentClient(options => options
            .Notifications(notifications => notifications.Categories(NotificationCategories.None))
            .Timeouts(timeouts => timeouts.Default(TimeSpan.FromMilliseconds(200))));
        var torrent = client.AttachTorrent(new TorrentInfo(Path.GetFullPath(Path.Combine("files", "big-buck-bunny.torrent"))), _tempSavePath);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await Assert.ThrowsAsync<TimeoutException>(() => torrent.RenameFileAsync(0, "renamed.srt", CancellationToken.None));
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"Took {stopwatch.Elapsed}.");

        stopwatch.Restart();
        await Assert.ThrowsAsync<TimeoutException>(() => torrent.RenameFileAsync(0, "renamed-again.srt", CancellationToken.None, TimeSpan.FromSeconds(1)));
        Assert.True(stopwatch.Elapsed >= TimeSpan.FromMilliseconds(900), $"Took {stopwatch.Elapsed}.");
    }

    [Fact]
    public void GetState_ContainsTheDhtState()
    {
        using var client = CreateWithDht(state: null);
        WaitUntil(() => client.IsDhtRunning);

        Assert.Contains("9:dht state", System.Text.Encoding.Latin1.GetString(client.GetState()));
    }

    [Fact]
    public void State_GivesTheRestartedDhtItsNodeIds()
    {
        byte[] first;
        using (var client = CreateWithDht(state: null))
        {
            WaitUntil(() => client.IsDhtRunning);
            first = client.GetState();
        }

        using var restored = CreateWithDht(first);
        WaitUntil(() => restored.IsDhtRunning);

        Assert.Equal(FirstDhtNodeId(first), FirstDhtNodeId(restored.GetState()));
    }

    // The DHT needs a socket that isn't loopback-only to run at all. The one bootstrap node is unroutable
    // (TEST-NET-1), so nothing leaves the machine - and having one matters: libtorrent then starts the DHT
    // only once the node's address is resolved, after the listen sockets are open. With none, it starts the
    // DHT before there are any sockets and drops the saved state unused.
    private static TorrentClient CreateWithDht(byte[]? state) => new(options => options
        .Network(net => net.ListenOn(System.Net.IPAddress.Any, 0))
        .Discovery(discovery => discovery.Dht(bootstrapNodes: ["192.0.2.1:6881"]).Lsd(false).Upnp(false).NatPmp(false))
        .State(state));

    // The first node id in the saved DHT state's "node-id" list (each entry is a 20-byte id followed by the
    // address it was used on).
    private static string FirstDhtNodeId(byte[] state)
    {
        var text = System.Text.Encoding.Latin1.GetString(state);
        var list = text.IndexOf("7:node-idl", StringComparison.Ordinal);
        Assert.True(list >= 0, "No DHT node ids in the state.");

        var lengthStart = list + "7:node-idl".Length;
        var colon = text.IndexOf(':', lengthStart);
        var length = int.Parse(text[lengthStart..colon], System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(length >= 20, "The node id entry is too short.");

        return Convert.ToHexString(state, colon + 1, 20);
    }

    [Fact]
    public void UnreadableState_IsIgnored()
    {
        using var client = new TorrentClient(options => options.State("not a session state"u8.ToArray()));

        Assert.NotEmpty(client.GetState());
    }

    private static void WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "The condition was not met in time.");
            Thread.Sleep(25);
        }
    }

    private static Task WaitUntilAsync(Func<bool> condition) => WaitUntilAsync(() => Task.FromResult(condition()));

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
        while (!await condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "The condition was not met in time.");
            await Task.Delay(25);
        }
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
        var clients = Enumerable.Range(0, clientCount).Select(_ => new TorrentClient()).ToArray();

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
                var port = ports[i];
                clients[i].ChangeOptions(options => options
                    .Notifications(notifications => notifications.Categories(NotificationCategories.Status))
                    .Network(net => net.ListenOn(System.Net.IPAddress.Loopback, port)));
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
    public void IpFilter_ReplacesAndClears_WithoutThrowing()
    {
        _client.ChangeOptions(options => options.IpFilter(filter => filter.Block("1.2.3.4", "1.2.3.4").Allow("1.2.3.4", "1.2.3.4")));
        _client.ChangeOptions(options => options.IpFilter(_ => { }));
    }

    [Fact]
    public void IpFilter_RejectsAnUnreadableRange()
    {
        Assert.Throws<FormatException>(() => _client.ChangeOptions(options => options.IpFilter(filter => filter.Block("not-an-ip", "1.2.3.4"))));
        Assert.Throws<ArgumentException>(() => _client.ChangeOptions(options => options.IpFilter(filter => filter.Block("1.2.3.4", "::1"))));
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