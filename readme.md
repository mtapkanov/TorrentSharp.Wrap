# TorrentSharp.Wrap
A .NET wrapper for libtorrent, with its own native layer (no external native package dependency).

## Building from source
Requires [CMake](https://cmake.org), [vcpkg](https://vcpkg.io) (`VCPKG_ROOT` pointing at your checkout, defaults to `~/vcpkg`), and the .NET SDK.

```bash
./scripts/build.sh
```

This configures and builds the native `tsw` library (first run only - fetches/builds `libtorrent-rasterbar` via vcpkg, which can take a few minutes), then builds `TorrentSharp.Wrap.sln`. Re-running only rebuilds what changed.

## Usage
Create a single `TorrentClient` instance.
This will usually be `static` (or singleton if using a dependency container), and a `TorrentClientConfig` can be passed in the constructor to configure the client.

```csharp
// create a new TorrentClient instance, optionally passing in the configuration
var options = new TorrentClientConfig
{
    ForceEncryption = true,
    MaxConnections = 500
};

using var client = new TorrentClient(options);

// bonus: there is also an event handler that can be subscribed to if more information is wanted.
client.NotificationRaised += (sender, notification) =>
{
    // notification can be checked against all classes in the TorrentSharp.Wrap.Notifications namespace for more properties.
    Console.WriteLine(notification.Message);
};
```

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
This method also has an overload allowing a custom save path to be specified. If not, the default save path will be used (`client.DefaultDownloadPath`).

```csharp
// this will be saved to DefaultDownloadPath.
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
async Task PerformDownload(TorrentClient client, TorrentInfo info, string? savePath = null)
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
`TorrentSharp.Wrap.DependencyInjection` registers a single `TorrentClient` against `Microsoft.Extensions.DependencyInjection`, configured through the options pattern.

```csharp
services.AddTorrentClient(config =>
{
    config.ForceEncryption = true;
    config.MaxConnections = 500;
});
```

### Supported Systems
Built and tested on macOS (arm64). Native build triplets exist for Linux and Windows (x64/arm64) with fully static linkage, but they haven't been validated outside of macOS yet.
