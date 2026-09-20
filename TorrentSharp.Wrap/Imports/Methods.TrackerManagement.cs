using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap.Imports;

internal static partial class Methods
{
    /// <summary>
    /// Adds a tracker to a torrent's announce list.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <param name="url">Tracker announce URL</param>
    /// <param name="tier">Tier the tracker belongs to - lower tiers are tried first</param>
    [LibraryImport(LibraryName, EntryPoint = "add_torrent_tracker", StringMarshalling = StringMarshalling.Utf8)]
    public static partial void AddTorrentTracker(IntPtr torrentSessionHandle, [MarshalAs(UnmanagedType.LPUTF8Str)] string url, byte tier);

    /// <summary>
    /// Replaces a torrent's entire announce list.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <param name="urls">Tracker announce URLs</param>
    /// <param name="tiers">Tier for each entry in <paramref name="urls"/>, by index</param>
    /// <param name="count">Length of <paramref name="urls"/> and <paramref name="tiers"/></param>
    [LibraryImport(LibraryName, EntryPoint = "replace_torrent_trackers", StringMarshalling = StringMarshalling.Utf8)]
    public static partial void ReplaceTorrentTrackers(IntPtr torrentSessionHandle, string[] urls, byte[] tiers, int count);

    /// <summary>
    /// Adds a BEP 19 (GetRight-style) web seed to a torrent.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <param name="url">Web seed URL</param>
    [LibraryImport(LibraryName, EntryPoint = "add_torrent_url_seed", StringMarshalling = StringMarshalling.Utf8)]
    public static partial void AddTorrentUrlSeed(IntPtr torrentSessionHandle, [MarshalAs(UnmanagedType.LPUTF8Str)] string url);

    /// <summary>
    /// Adds a BEP 17 (Hoffman-style) web seed to a torrent.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <param name="url">Web seed URL</param>
    [LibraryImport(LibraryName, EntryPoint = "add_torrent_http_seed", StringMarshalling = StringMarshalling.Utf8)]
    public static partial void AddTorrentHttpSeed(IntPtr torrentSessionHandle, [MarshalAs(UnmanagedType.LPUTF8Str)] string url);

    /// <summary>
    /// Sends a scrape request to a tracker. Completion is reported asynchronously via a
    /// <see cref="Enums.NotificationType.Scrape"/> notification.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <param name="trackerIndex">Index of the tracker to scrape, or <c>-1</c> for the last working tracker</param>
    [LibraryImport(LibraryName, EntryPoint = "scrape_torrent_tracker")]
    public static partial void ScrapeTorrentTracker(IntPtr torrentSessionHandle, int trackerIndex);
}
