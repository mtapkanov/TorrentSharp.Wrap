using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap.Imports;

internal static partial class Methods
{
    /// <summary>
    /// Reads a torrent's upload bandwidth limit, in bytes/sec.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <returns><c>0</c> means unlimited</returns>
    [LibraryImport(LibraryName, EntryPoint = "get_torrent_upload_limit")]
    public static partial int GetTorrentUploadLimit(IntPtr torrentSessionHandle);

    /// <summary>
    /// Sets a torrent's upload bandwidth limit, in bytes/sec.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <param name="limit">Limit in bytes/sec; <c>0</c> means unlimited</param>
    [LibraryImport(LibraryName, EntryPoint = "set_torrent_upload_limit")]
    public static partial void SetTorrentUploadLimit(IntPtr torrentSessionHandle, int limit);

    /// <summary>
    /// Reads a torrent's download bandwidth limit, in bytes/sec.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <returns><c>0</c> means unlimited</returns>
    [LibraryImport(LibraryName, EntryPoint = "get_torrent_download_limit")]
    public static partial int GetTorrentDownloadLimit(IntPtr torrentSessionHandle);

    /// <summary>
    /// Sets a torrent's download bandwidth limit, in bytes/sec.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <param name="limit">Limit in bytes/sec; <c>0</c> means unlimited</param>
    [LibraryImport(LibraryName, EntryPoint = "set_torrent_download_limit")]
    public static partial void SetTorrentDownloadLimit(IntPtr torrentSessionHandle, int limit);

    /// <summary>
    /// Reads a torrent's position in the session's download queue.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <returns><c>-1</c> if the torrent isn't queued</returns>
    [LibraryImport(LibraryName, EntryPoint = "get_torrent_queue_position")]
    public static partial int GetTorrentQueuePosition(IntPtr torrentSessionHandle);

    /// <summary>
    /// Moves a torrent one position up (earlier) in the download queue.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    [LibraryImport(LibraryName, EntryPoint = "torrent_queue_position_up")]
    public static partial void TorrentQueuePositionUp(IntPtr torrentSessionHandle);

    /// <summary>
    /// Moves a torrent one position down (later) in the download queue.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    [LibraryImport(LibraryName, EntryPoint = "torrent_queue_position_down")]
    public static partial void TorrentQueuePositionDown(IntPtr torrentSessionHandle);

    /// <summary>
    /// Moves a torrent to the top of the download queue.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    [LibraryImport(LibraryName, EntryPoint = "torrent_queue_position_top")]
    public static partial void TorrentQueuePositionTop(IntPtr torrentSessionHandle);

    /// <summary>
    /// Moves a torrent to the bottom of the download queue.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    [LibraryImport(LibraryName, EntryPoint = "torrent_queue_position_bottom")]
    public static partial void TorrentQueuePositionBottom(IntPtr torrentSessionHandle);

    /// <summary>
    /// Reads whether a torrent downloads pieces in order (roughly sequential) rather than rarest-first.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    [return: MarshalAs(UnmanagedType.I1)]
    [LibraryImport(LibraryName, EntryPoint = "get_torrent_sequential_download")]
    public static partial bool GetTorrentSequentialDownload(IntPtr torrentSessionHandle);

    /// <summary>
    /// Sets whether a torrent downloads pieces in order (roughly sequential) rather than rarest-first.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <param name="value">New value</param>
    [LibraryImport(LibraryName, EntryPoint = "set_torrent_sequential_download")]
    public static partial void SetTorrentSequentialDownload(IntPtr torrentSessionHandle, [MarshalAs(UnmanagedType.I1)] bool value);

    /// <summary>
    /// Reads whether a finished torrent is in super-seeding mode.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    [return: MarshalAs(UnmanagedType.I1)]
    [LibraryImport(LibraryName, EntryPoint = "get_torrent_super_seeding")]
    public static partial bool GetTorrentSuperSeeding(IntPtr torrentSessionHandle);

    /// <summary>
    /// Sets whether a finished torrent is in super-seeding mode.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <param name="value">New value</param>
    [LibraryImport(LibraryName, EntryPoint = "set_torrent_super_seeding")]
    public static partial void SetTorrentSuperSeeding(IntPtr torrentSessionHandle, [MarshalAs(UnmanagedType.I1)] bool value);

    /// <summary>
    /// Reads whether a torrent is in share mode (optimizes for improving the swarm's ratio over
    /// completing the download).
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    [return: MarshalAs(UnmanagedType.I1)]
    [LibraryImport(LibraryName, EntryPoint = "get_torrent_share_mode")]
    public static partial bool GetTorrentShareMode(IntPtr torrentSessionHandle);

    /// <summary>
    /// Sets whether a torrent is in share mode (optimizes for improving the swarm's ratio over
    /// completing the download).
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <param name="value">New value</param>
    [LibraryImport(LibraryName, EntryPoint = "set_torrent_share_mode")]
    public static partial void SetTorrentShareMode(IntPtr torrentSessionHandle, [MarshalAs(UnmanagedType.I1)] bool value);

    /// <summary>
    /// Reads whether a torrent will only upload, never download (even if incomplete).
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    [return: MarshalAs(UnmanagedType.I1)]
    [LibraryImport(LibraryName, EntryPoint = "get_torrent_upload_mode")]
    public static partial bool GetTorrentUploadMode(IntPtr torrentSessionHandle);

    /// <summary>
    /// Sets whether a torrent will only upload, never download (even if incomplete).
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <param name="value">New value</param>
    [LibraryImport(LibraryName, EntryPoint = "set_torrent_upload_mode")]
    public static partial void SetTorrentUploadMode(IntPtr torrentSessionHandle, [MarshalAs(UnmanagedType.I1)] bool value);
}
