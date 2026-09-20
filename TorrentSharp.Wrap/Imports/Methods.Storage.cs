using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap.Imports;

internal static partial class Methods
{
    /// <summary>
    /// Triggers a full recheck of a torrent's data on disk. Progress is reported through the
    /// existing <see cref="Enums.NotificationType.TorrentStatus"/> notification's checking states.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    [LibraryImport(LibraryName, EntryPoint = "force_recheck")]
    public static partial void ForceRecheck(IntPtr torrentSessionHandle);

    /// <summary>
    /// Triggers a rename of one of a torrent's files. Completion is reported asynchronously via a
    /// <see cref="Enums.NotificationType.FileRenamed"/> notification.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <param name="fileIndex">Index of the file to rename</param>
    /// <param name="newName">New name for the file</param>
    [LibraryImport(LibraryName, EntryPoint = "rename_torrent_file", StringMarshalling = StringMarshalling.Utf8)]
    public static partial void RenameTorrentFile(IntPtr torrentSessionHandle, int fileIndex, [MarshalAs(UnmanagedType.LPUTF8Str)] string newName);

    /// <summary>
    /// Triggers a move of a torrent's storage to a new path. Completion is reported asynchronously
    /// via a <see cref="Enums.NotificationType.StorageMoved"/> notification.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <param name="newPath">Destination directory for the torrent's contents</param>
    [LibraryImport(LibraryName, EntryPoint = "move_torrent_storage", StringMarshalling = StringMarshalling.Utf8)]
    public static partial void MoveTorrentStorage(IntPtr torrentSessionHandle, [MarshalAs(UnmanagedType.LPUTF8Str)] string newPath);
}
