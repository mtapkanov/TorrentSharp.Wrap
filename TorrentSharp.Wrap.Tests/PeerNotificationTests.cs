using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using TorrentSharp.Wrap.Configurations;
using TorrentSharp.Wrap.Configurations.Settings;
using TorrentSharp.Wrap.Enums;
using TorrentSharp.Wrap.Notifications;
using TorrentSharp.Wrap.Utils;

namespace TorrentSharp.Wrap.Tests;

/// <summary>
/// Two clients on loopback and a tiny HTTP tracker that points the second at the first: no internet, no DHT.
/// The native side used to call <c>address::to_v6()</c> on the peer's endpoint, which throws for an IPv4
/// address; the dispatcher's catch-all swallowed it, so no IPv4 peer (nearly every peer) ever produced a
/// <see cref="PeerNotification"/>.
/// </summary>
public class PeerNotificationTests
{
    private const int PieceLength = 16_384;

    private static int FreeLoopbackPort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    private static byte[] BencodeBytes(byte[] value) => [.. Encoding.ASCII.GetBytes($"{value.Length}:"), .. value];

    private static byte[] BencodeString(string value) => BencodeBytes(Encoding.UTF8.GetBytes(value));

    private static byte[] Concat(params byte[][] parts) => [.. parts.SelectMany(part => part)];

    private static byte[] BuildTorrent(string announceUrl, string name, byte[] content)
    {
        var pieceCount = (content.Length + PieceLength - 1) / PieceLength;
        var pieces = new byte[pieceCount * 20];
        for (var i = 0; i < pieceCount; i++)
        {
            var offset = i * PieceLength;
            SHA1.HashData(content.AsSpan(offset, Math.Min(PieceLength, content.Length - offset)), pieces.AsSpan(i * 20, 20));
        }

        // Dictionary keys must be in byte order: announce, info{length,name,piece length,pieces}.
        var info = Concat(
            "d"u8.ToArray(),
            BencodeString("length"), Encoding.ASCII.GetBytes($"i{content.Length}e"),
            BencodeString("name"), BencodeString(name),
            BencodeString("piece length"), Encoding.ASCII.GetBytes($"i{PieceLength}e"),
            BencodeString("pieces"), BencodeBytes(pieces),
            "e"u8.ToArray());

        return Concat("d"u8.ToArray(), BencodeString("announce"), BencodeString(announceUrl), BencodeString("info"), info, "e"u8.ToArray());
    }

    [Fact]
    public async Task ConnectingToAnIPv4Peer_RaisesAPeerNotification_WithTheMappedAddress()
    {
        var content = new byte[3 * PieceLength];
        new Random(4242).NextBytes(content);

        var seederPort = FreeLoopbackPort();
        var trackerPort = FreeLoopbackPort();
        var seedDirectory = Directory.CreateTempSubdirectory("tsw-peer-seed-").FullName;
        var leechDirectory = Directory.CreateTempSubdirectory("tsw-peer-leech-").FullName;
        await File.WriteAllBytesAsync(Path.Combine(seedDirectory, "peer-test.bin"), content);

        // The tracker's only job: tell whoever announces that the seeder is at 127.0.0.1:seederPort (compact form).
        using var tracker = new HttpListener();
        tracker.Prefixes.Add($"http://127.0.0.1:{trackerPort}/");
        tracker.Start();
        var trackerLoop = Task.Run(async () =>
        {
            while (tracker.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = await tracker.GetContextAsync();
                }
                catch (Exception)
                {
                    return;
                }

                var peer = new byte[] { 127, 0, 0, 1, (byte)(seederPort >> 8), (byte)seederPort };
                var body = Concat("d8:intervali30e5:peers6:"u8.ToArray(), peer, "e"u8.ToArray());
                context.Response.ContentType = "text/plain";
                context.Response.ContentLength64 = body.Length;
                await context.Response.OutputStream.WriteAsync(body);
                context.Response.Close();
            }
        });

        var categories = NotificationCategories.Status | NotificationCategories.Tracker | NotificationCategories.Connect | NotificationCategories.Peer;
        using var seeder = new TorrentClient(new TorrentClientConfig { NotificationCategories = categories });
        using var leecher = new TorrentClient(new TorrentClientConfig { NotificationCategories = categories });

        var connected = new TaskCompletionSource<PeerNotification>(TaskCreationOptions.RunContinuationsAsynchronously);
        leecher.NotificationRaised += (_, notification) =>
        {
            if (notification is PeerNotification { NotificationType: PeerNotificationType.ConnectedOutgoing } peer)
                connected.TrySetResult(peer);
        };

        try
        {
            seeder.UpdateSettings(new SettingsPack().SetListenInterfaces(new IpInterface(new IPEndPoint(IPAddress.Loopback, seederPort))));

            var torrentBytes = BuildTorrent($"http://127.0.0.1:{trackerPort}/announce", "peer-test.bin", content);
            var seedManager = seeder.AttachTorrent(new TorrentInfo(torrentBytes), seedDirectory);
            seedManager.Start();
            var leechManager = leecher.AttachTorrent(new TorrentInfo(torrentBytes), leechDirectory);
            leechManager.Start();

            var peer = await connected.Task.WaitAsync(TimeSpan.FromSeconds(30));

            // Both clients live on this machine; libtorrent may report the peer as loopback or as one of the
            // machine's own addresses, so any of them is right - what matters is that it is a real IPv4
            // address in mapped form, not garbage and not a dropped alert.
            Assert.True(peer.Address.IsIPv4MappedToIPv6);
            var machineAddresses = NetworkInterface.GetAllNetworkInterfaces()
                .SelectMany(adapter => adapter.GetIPProperties().UnicastAddresses)
                .Select(unicast => unicast.Address)
                .Append(IPAddress.Loopback);
            Assert.Contains(peer.Address.MapToIPv4(), machineAddresses);
            Assert.Equal(leechManager.InfoHash, peer.TorrentManager.InfoHash, StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            tracker.Stop();
            await trackerLoop;
            Directory.Delete(seedDirectory, true);
            Directory.Delete(leechDirectory, true);
        }
    }
}
