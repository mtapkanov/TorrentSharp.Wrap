using System.Runtime.InteropServices;
using TorrentSharp.Wrap.Imports.Structs;

namespace TorrentSharp.Wrap.Imports;

internal static partial class Methods
{
    /// <summary>
    /// Parses a .torrent file from disk.
    /// </summary>
    /// <param name="path">Path of the file to parse</param>
    /// <returns>Handle to the parsed torrent, or <see cref="IntPtr.Zero"/> on failure</returns>
    [LibraryImport(LibraryName, EntryPoint = "create_torrent_file", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr CreateTorrentFromFile([MarshalAs(UnmanagedType.LPUTF8Str)] string path);

    /// <summary>
    /// Parses a .torrent file from an in-memory byte array.
    /// </summary>
    /// <param name="content">Buffer holding the torrent file's contents</param>
    /// <param name="length">Size of the buffer</param>
    /// <returns>Handle to the parsed torrent, or <see cref="IntPtr.Zero"/> on failure</returns>
    [LibraryImport(LibraryName, EntryPoint = "create_torrent_bytes")]
    public static partial IntPtr CreateTorrentFromBytes(byte[] content, long length);

    /// <summary>
    /// Parses a .torrent file from an unmanaged memory region.
    /// </summary>
    /// <param name="content">Pointer to the first byte of the torrent file's contents</param>
    /// <param name="length">Size of the region</param>
    /// <returns>Handle to the parsed torrent, or <see cref="IntPtr.Zero"/> on failure</returns>
    [LibraryImport(LibraryName, EntryPoint = "create_torrent_bytes")]
    public static partial IntPtr CreateTorrentFromBytes(IntPtr content, long length);

    /// <summary>
    /// Releases the unmanaged resources held by a parsed torrent.
    /// </summary>
    /// <param name="torrentHandle">
    /// Handle obtained from <see cref="CreateTorrentFromFile"/> or <see cref="CreateTorrentFromBytes"/>
    /// </param>
    [LibraryImport(LibraryName, EntryPoint = "destroy_torrent")]
    public static partial void FreeTorrent(IntPtr torrentHandle);

    /// <summary>
    /// Adds a parsed torrent to a session so it can be downloaded.
    /// </summary>
    /// <param name="sessionHandle">Session to attach the torrent to</param>
    /// <param name="torrentHandle">Handle of the parsed torrent</param>
    /// <param name="savePath">Destination directory for the torrent's contents</param>
    /// <returns>Handle scoped to this torrent within the session</returns>
    [LibraryImport(LibraryName, EntryPoint = "attach_torrent", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr AttachTorrent(IntPtr sessionHandle, IntPtr torrentHandle, [MarshalAs(UnmanagedType.LPUTF8Str)] string savePath);

    /// <summary>
    /// Adds a magnet link to a session; its metadata is fetched from peers afterward.
    /// A <see cref="Enums.NotificationType.MetadataReceived"/> notification fires once it arrives.
    /// </summary>
    /// <param name="sessionHandle">Session to attach to</param>
    /// <param name="magnetUri">Magnet URI to parse and add</param>
    /// <param name="savePath">Destination directory for the torrent's contents</param>
    /// <returns>Handle scoped to this torrent within the session, or <see cref="IntPtr.Zero"/> if the URI couldn't be parsed</returns>
    [LibraryImport(LibraryName, EntryPoint = "attach_magnet", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr AttachMagnet(IntPtr sessionHandle, [MarshalAs(UnmanagedType.LPUTF8Str)] string magnetUri, [MarshalAs(UnmanagedType.LPUTF8Str)] string savePath);

    /// <summary>
    /// Retrieves the parsed torrent behind a session-scoped handle, once its metadata has arrived.
    /// The result must be released with <see cref="FreeTorrent"/>.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <returns>Handle to an <c>lt::torrent_info</c>, or <see cref="IntPtr.Zero"/> if metadata hasn't arrived yet</returns>
    [LibraryImport(LibraryName, EntryPoint = "get_handle_torrent_info")]
    public static partial IntPtr GetHandleTorrentInfo(IntPtr torrentSessionHandle);

    /// <summary>
    /// Writes the v1 info-hash of a torrent into a caller-supplied buffer.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <param name="hashOut">Buffer that receives the 20-byte info-hash</param>
    [LibraryImport(LibraryName, EntryPoint = "get_torrent_handle_info_hash")]
    public static partial void GetTorrentHandleInfoHash(IntPtr torrentSessionHandle, Span<byte> hashOut);

    /// <summary>
    /// Removes a torrent from a session and stops its transfer.
    /// </summary>
    /// <param name="sessionHandle">Session to remove the torrent from</param>
    /// <param name="torrentSessionHandle">Session-scoped handle of the torrent to remove</param>
    /// <remarks>
    /// Once this call returns, <paramref name="sessionHandle"/> is no longer valid for further use.
    /// </remarks>
    [LibraryImport(LibraryName, EntryPoint = "detach_torrent")]
    public static partial void DetachTorrent(IntPtr sessionHandle, IntPtr torrentSessionHandle);

    /// <summary>
    /// Reads the parsed metadata for a torrent.
    /// </summary>
    /// <param name="torrentHandle">Handle of the parsed torrent</param>
    /// <returns>Handle to a <see cref="MetadataInfo"/> struct describing the torrent</returns>
    /// <remarks>Release the result with <see cref="FreeTorrentInfo"/> once you're done with it.</remarks>
    [LibraryImport(LibraryName, EntryPoint = "get_torrent_info")]
    public static partial IntPtr GetTorrentInfo(IntPtr torrentHandle);

    /// <summary>
    /// Releases a handle previously returned by <see cref="GetTorrentInfo"/>.
    /// </summary>
    /// <param name="torrentInfoHandle">Handle to release</param>
    [LibraryImport(LibraryName, EntryPoint = "destroy_torrent_info")]
    public static partial void FreeTorrentInfo(IntPtr torrentInfoHandle);

    /// <summary>
    /// Writes a torrent's metadata back out as a .torrent file.
    /// </summary>
    /// <param name="torrentHandle">Handle of the parsed torrent</param>
    /// <param name="filePath">Destination path for the .torrent file</param>
    /// <returns><c>true</c> on success; <c>false</c> otherwise</returns>
    [return: MarshalAs(UnmanagedType.I1)]
    [LibraryImport(LibraryName, EntryPoint = "save_torrent_to_file", StringMarshalling = StringMarshalling.Utf8)]
    public static partial bool SaveTorrentToFile(IntPtr torrentHandle, [MarshalAs(UnmanagedType.LPUTF8Str)] string filePath);

    /// <summary>
    /// Serializes a torrent's metadata into a native-allocated byte buffer.
    /// The buffer must be released with <see cref="FreeTorrentBytes"/>.
    /// </summary>
    /// <param name="torrentHandle">Handle of the parsed torrent</param>
    /// <param name="data">Receives a pointer to the allocated buffer</param>
    /// <param name="size">Receives the buffer's size in bytes</param>
    [LibraryImport(LibraryName, EntryPoint = "get_torrent_bytes")]
    public static partial void GetTorrentBytes(IntPtr torrentHandle, out IntPtr data, out long size);

    /// <summary>
    /// Releases a buffer previously returned by <see cref="GetTorrentBytes"/>.
    /// </summary>
    /// <param name="data">Buffer to release</param>
    [LibraryImport(LibraryName, EntryPoint = "free_torrent_bytes")]
    public static partial void FreeTorrentBytes(IntPtr data);

    /// <summary>
    /// Begins or resumes downloading/seeding a torrent.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle to resume</param>
    [LibraryImport(LibraryName, EntryPoint = "start_torrent")]
    public static partial void StartTorrent(IntPtr torrentSessionHandle);

    /// <summary>
    /// Pauses a torrent's transfer.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle to pause</param>
    [LibraryImport(LibraryName, EntryPoint = "stop_torrent")]
    public static partial void StopTorrent(IntPtr torrentSessionHandle);

    /// <summary>
    /// Forces a fresh announce to every tracker for a torrent.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle to reannounce</param>
    /// <param name="seconds">Delay, in seconds, before the reannounce fires</param>
    /// <param name="force">Whether to bypass the minimum interval between announces</param>
    [LibraryImport(LibraryName, EntryPoint = "reannounce_torrent")]
    public static partial void ReannounceTorrent(IntPtr torrentSessionHandle, int seconds, [MarshalAs(UnmanagedType.I1)] bool force);

    /// <summary>
    /// Reads a torrent's current status.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle to query</param>
    /// <param name="status">Populated with the current status on return</param>
    [LibraryImport(LibraryName, EntryPoint = "get_torrent_status")]
    public static partial void GetTorrentStatus(IntPtr torrentSessionHandle, out TorrentStatus status);
}
