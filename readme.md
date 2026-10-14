# TorrentSharp.Wrap
[![NuGet](https://img.shields.io/nuget/v/TorrentSharp.Wrap.svg?label=TorrentSharp.Wrap)](https://www.nuget.org/packages/TorrentSharp.Wrap/)
[![NuGet](https://img.shields.io/nuget/v/TorrentSharp.Wrap.DependencyInjection.svg?label=TorrentSharp.Wrap.DependencyInjection)](https://www.nuget.org/packages/TorrentSharp.Wrap.DependencyInjection/)
[![NuGet](https://img.shields.io/nuget/v/TorrentSharp.Wrap.Native.svg?label=TorrentSharp.Wrap.Native)](https://www.nuget.org/packages/TorrentSharp.Wrap.Native/)

A .NET wrapper for libtorrent, with its own native layer (no external native package dependency).

## Installation

```bash
dotnet add package TorrentSharp.Wrap
```

`TorrentSharp.Wrap.Native` (the native `libtsw`/`libtorrent` binaries) comes along automatically as a dependency - it shouldn't normally be referenced directly.

## Building from source
Requires [CMake](https://cmake.org), [vcpkg](https://vcpkg.io) (`VCPKG_ROOT` pointing at your checkout, defaults to `~/vcpkg`), and the .NET SDK.

```bash
./scripts/build.sh
```

This configures and builds the native `tsw` library (first run only - fetches/builds `libtorrent-rasterbar` via vcpkg, which can take a few minutes), then builds `TorrentSharp.Wrap.sln`. Re-running only rebuilds what changed.

## Usage
Create a single `TorrentClient` (usually a singleton, or registered through dependency injection - see below). Everything it is configured with is given once, as it is created; anything not set keeps libtorrent's own default.

```csharp
using var client = new TorrentClient(options => options
    .Identity(id => id.UserAgent("MyApp/1.0").HandshakeClientVersion("MyApp/1.0"))
    .Network(net => net.ListenOn(IPAddress.Any, 51413))
    .Discovery(discovery => discovery.Dht().Lsd())
    .Limits(limits => limits.Connections(500))
    .Privacy(privacy => privacy.Encryption(EncryptionPolicy.Forced))
    // libtorrent only generates the alert categories asked for here, and the library adds none itself.
    // These three are what the async helpers need: Status for WaitForMetadata, Tracker for
    // ScrapeTrackerAsync, Storage for SaveResumeDataAsync/RenameFileAsync/MoveStorageAsync/ReadPieceAsync.
    // Without the matching category those calls wait until they time out. Add more (PieceProgress,
    // PortMapping, Connect, Peer, ...) for the notifications you want to receive.
    .Notifications(notifications => notifications.Categories(
        NotificationCategories.Status | NotificationCategories.Storage | NotificationCategories.Tracker))
    // subscribed before the session starts, so it also sees the listen/port-mapping notifications raised
    // while it does - one subscribed to client.NotificationRaised afterwards can miss those.
    .OnNotification((sender, notification) => Console.WriteLine(notification.Message)));
```

| Options | Sets (libtorrent setting, default) |
|---|---|
| `Identity` | `UserAgent` (`user_agent`, `libtorrent/2.0.11`), `HandshakeClientVersion` (`handshake_client_version`, the user agent), `PeerFingerprint` (`peer_fingerprint`, `-LT20B0-`) |
| `Network` | `ListenOn` (`listen_interfaces`, `0.0.0.0:6881,[::]:6881`), `Tcp` / `Utp` (`enable_incoming/outgoing_tcp/utp`, all on) |
| `Discovery` | `Dht` (`enable_dht`, on), `DhtBootstrapNodes` (`dht_bootstrap_nodes`, `dht.libtorrent.org:25401`), `Lsd` / `Upnp` / `NatPmp` (`enable_lsd` / `enable_upnp` / `enable_natpmp`, all on) |
| `Limits` | `Download` / `Upload` (`download_rate_limit` / `upload_rate_limit`, 0 = unlimited), `Connections` (`connections_limit`, 200), `Active` (`active_downloads` 3 / `active_seeds` 5 / `active_limit` 500 - auto-managed torrents only), `SeedingOutgoingConnections` (on) |
| `Privacy` | `AnonymousMode` (`anonymous_mode`, off), `Encryption` (`in_enc_policy` / `out_enc_policy`, `Enabled`) |
| `Notifications` | `Categories` (`alert_mask`, `Error`), `IncludeUnmapped` (raise a bare `SessionNotification` for types with no class of their own, off) |
| `Storage` | `DefaultDownloadPath` (`downloads` under the current directory) |
| `Timeouts` | `Default` - how long the async methods wait when not given a timeout (30 s) |
| `IpFilter` | `Block` / `Allow` ranges - replaces the whole filter |
| `Advanced` | any of the other typed settings in `TorrentSharp.Wrap.Configurations.Settings`, e.g. `pack.Set(new ActiveChecking(2))` |

### Changing options while running
`ChangeOptions` takes the same options and changes only what it is given:

```csharp
client.ChangeOptions(options => options
    .Network(net => net.ListenOn(IPAddress.Any, 6882))
    .Limits(limits => limits.Download(0).Upload(512 * 1024)));
```

### Keeping the DHT state
`GetState()` returns the session's DHT state (node ids and known nodes). Pass it back on the next start, and the DHT carries on from there instead of bootstrapping from scratch:

```csharp
File.WriteAllBytes("session.state", client.GetState());

// next time
var state = File.Exists("session.state") ? File.ReadAllBytes("session.state") : null;
using var client = new TorrentClient(options => options.State(state));
```

libtorrent only uses the state when the DHT starts after the listen sockets are open - true while there are bootstrap nodes to resolve first, which is the default. With the bootstrap nodes cleared, the state is dropped.

.torrent files can be parsed either by passing in a file path or a byte array containing the file contents to the `TorrentInfo` class, and the instance will be populated accordingly.
Metadata is available via `TorrentInfo.Metadata`, such as the name of the torrent, and `TorrentInfo.Files` lists what it contains.

```csharp
var filePath = "path/to/torrent/file.torrent";
var torrentInfo = new TorrentInfo(filePath);

// get the name and list of files
Console.WriteLine($"Name: {torrentInfo.Metadata.Name}");
Console.WriteLine("Files:");

foreach (var file in torrentInfo.Files)
{
    Console.WriteLine($"- {file.Path} ({file.FileSize} bytes)");
}
```

After parsing a torrent file, it can be "attached" to the client to start downloading the files. Note this method will not start a download, but will prepare the client to download the files when `Start` is called.
This method also has an overload allowing a custom save path to be specified. If not, the default save path will be used (`Storage(s => s.DefaultDownloadPath(...))`, by default `downloads` under the current directory).

```csharp
// this will be saved to the default download path.
var torrentManager = client.AttachTorrent(torrentInfo);

// the metadata can still be accessed but files have additional properties including their final destination and their download priority, which can be changed.
torrentManager.Files[0].Priority = FileDownloadPriority.DoNotDownload;

// after setting priorities, the download can begin (or resume if the torrent was previously started)
torrentManager.Start();

// if we want a progress update, we can request one
var progress = torrentManager.GetCurrentStatus();

if (progress.State == TorrentState.Finished)
{
    torrentManager.Stop();

    // when we want to "dispose" the manager, we can detach it from the client
    client.DetachTorrent(torrentManager);
}
```

If we want to wait for the download to complete, a timer and a `TaskCompletionSource` can be used to await the completion of the download.

```csharp
async Task PerformDownload(ITorrentClient client, TorrentInfo info, string? savePath = null)
{
    var torrentTransfer = new TaskCompletionSource();
    var torrentManager = client.AttachTorrent(info, savePath);

    torrentManager.Start();

    using (new Timer(PerformProgressCheck, null, TimeSpan.Zero, TimeSpan.FromSeconds(1)))
    {
        await torrentTransfer.Task;
    }

    // at this point, the torrentManager has either finished downloading or seeding, and the poll has been stopped.

    client.DetachTorrent(torrentManager);
    return;

    // this function polls the progress (makes a call to libtorrent) every second to check if the torrent has finished downloading
    void PerformProgressCheck(object? state)
    {
        if (torrentManager.GetCurrentStatus().State is TorrentState.Seeding or TorrentState.Finished)
        {
            torrentTransfer.SetResult();
        }
    }
}
```

### Magnet links
Magnet links are attached the same way, but metadata isn't available immediately - `TorrentManager.Info` stays `null` (and `Files` stays empty) until it's fetched from the swarm. `WaitForMetadata` waits for it, bounded by a `CancellationToken`.

```csharp
var torrentManager = client.AttachMagnet(magnetUri);
torrentManager.Start();

using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));
await torrentManager.WaitForMetadata(cancellationTokenSource.Token);

// metadata (and Files) are now populated
Console.WriteLine($"Name: {torrentManager.Info!.Metadata.Name}");
```

## Dependency injection

```bash
dotnet add package TorrentSharp.Wrap.DependencyInjection
```

[`TorrentSharp.Wrap.DependencyInjection`](https://www.nuget.org/packages/TorrentSharp.Wrap.DependencyInjection/) registers the client as an `ITorrentClient` singleton, with all of its configuration in one place. The overload with the service provider is for configuration that comes from other services - settings kept in a database, a notification handler, a saved state:

```csharp
services.AddTorrentClient((provider, options) => options
    .Identity(id => id.UserAgent("MyApp/1.0"))
    .Discovery(discovery => discovery.Dht().Lsd())
    .Notifications(notifications => notifications.Categories(NotificationCategories.Status | NotificationCategories.Storage))
    .FromConfiguration(configuration.GetSection("TorrentClient"))   // appsettings may override the values above
    .OnNotification(provider.GetRequiredService<MyNotificationHandler>().Handle)
    .State(provider.GetRequiredService<MyStateStore>().Load()));
```

The client is created on first use. Inject `ITorrentClient` and call `ChangeOptions` to change settings later.

`FromConfiguration` reads the same options from configuration; only the keys present are applied, at its place in the chain, and a list replaces the one set before it:

```json
"TorrentClient": {
  "Identity": { "UserAgent": "MyApp/1.0" },
  "Network": { "ListenInterfaces": [ "0.0.0.0:51413", "[::]:51413" ] },
  "Discovery": { "DhtBootstrapNodes": [ "router.bittorrent.com:6881" ] },
  "Limits": { "Connections": 500 },
  "Privacy": { "Encryption": "Forced" },
  "Notifications": { "Categories": "Status, Storage" },
  "Timeouts": { "Default": "00:00:45" },
  "IpFilter": { "Rules": [ { "First": "10.0.0.0", "Last": "10.255.255.255", "Blocked": true } ] }
}
```

### Supported Systems
Native builds (fully statically linked) are published for macOS, Linux, and Windows, on both x64 and arm64.
