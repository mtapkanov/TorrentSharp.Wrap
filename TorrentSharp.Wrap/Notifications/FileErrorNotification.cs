using TorrentSharp.Wrap.Imports.Events;

namespace TorrentSharp.Wrap.Notifications;

/// <summary>
/// Raised when libtorrent's storage layer fails to read or write a file the torrent needs (a
/// missing or inaccessible file, a full disk, a permissions issue, ...) - libtorrent auto-pauses
/// the torrent when this happens, so without this notification a torrent stuck this way looks
/// indistinguishable from one that's merely slow (still Checking/Downloading, 0% CPU, no progress,
/// no explanation).
/// </summary>
public class FileErrorNotification(FileErrorEvent @event, TorrentManager torrentManager) : SessionNotification(@event.Info)
{
    public TorrentManager TorrentManager { get; } = torrentManager;

    /// <summary>The underlying OS error code (an errno-equivalent on POSIX, e.g. ENOENT for a missing file).</summary>
    public int ErrorValue { get; } = @event.ErrorValue;

    /// <summary>
    /// Which file operation failed, as libtorrent's raw operation_t (see libtorrent/operations.hpp)
    /// - not re-declared as a .NET enum here since most of its ~40 values are socket-related and
    /// never apply to this alert; the handful that do (file, file_read, file_write, file_stat, ...)
    /// are already spelled out in <see cref="SessionNotification.Message"/>.
    /// </summary>
    public byte Operation { get; } = @event.Operation;

    /// <summary>The path libtorrent associates with the failure.</summary>
    public string Filename { get; } = @event.Filename;
}
