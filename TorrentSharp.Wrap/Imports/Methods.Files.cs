using System.Runtime.InteropServices;
using TorrentSharp.Wrap.Enums;
using TorrentSharp.Wrap.Imports.Structs;

namespace TorrentSharp.Wrap.Imports;

internal static partial class Methods
{
    /// <summary>
    /// Fetches the list of files contained in a torrent.
    /// </summary>
    /// <param name="torrentHandle">Handle of the parsed torrent to inspect</param>
    /// <param name="files">Populated with the resulting <see cref="TorrentFileList"/></param>
    [LibraryImport(LibraryName, EntryPoint = "get_torrent_file_list")]
    public static partial void GetTorrentFileList(IntPtr torrentHandle, out TorrentFileList files);

    /// <summary>
    /// Releases the unmanaged resources held by a <paramref name="files"/> list.
    /// </summary>
    /// <param name="files">List to release</param>
    [LibraryImport(LibraryName, EntryPoint = "destroy_torrent_file_list")]
    public static partial void FreeTorrentFileList(ref TorrentFileList files);

    /// <summary>
    /// Reads the download priority assigned to a file in a torrent.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <param name="fileIndex">Index of the file to query</param>
    [LibraryImport(LibraryName, EntryPoint = "get_file_dl_priority")]
    public static partial FileDownloadPriority GetFilePriority(IntPtr torrentSessionHandle, int fileIndex);

    /// <summary>
    /// Assigns a download priority to a file in a torrent.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <param name="fileIndex">Index of the file to update</param>
    /// <param name="priority">Priority to assign</param>
    [LibraryImport(LibraryName, EntryPoint = "set_file_dl_priority")]
    public static partial void SetFilePriority(IntPtr torrentSessionHandle, int fileIndex, FileDownloadPriority priority);
}
