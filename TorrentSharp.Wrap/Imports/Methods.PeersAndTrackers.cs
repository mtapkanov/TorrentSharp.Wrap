using System.Runtime.InteropServices;
using TorrentSharp.Wrap.Imports.Structs;

namespace TorrentSharp.Wrap.Imports;

internal static partial class Methods
{
    /// <summary>
    /// Fetches connection details for every peer currently connected to a torrent.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <param name="peers">Populated with the resulting <see cref="PeerList"/></param>
    [LibraryImport(LibraryName, EntryPoint = "get_torrent_peers")]
    public static partial void GetTorrentPeers(IntPtr torrentSessionHandle, out PeerList peers);

    /// <summary>
    /// Releases the unmanaged resources held by a <paramref name="peers"/> list.
    /// </summary>
    /// <param name="peers">List to release</param>
    [LibraryImport(LibraryName, EntryPoint = "destroy_torrent_peer_list")]
    public static partial void FreeTorrentPeerList(ref PeerList peers);

    /// <summary>
    /// Fetches announce state for every tracker attached to a torrent, across all tiers.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <param name="trackers">Populated with the resulting <see cref="TrackerList"/></param>
    [LibraryImport(LibraryName, EntryPoint = "get_torrent_trackers")]
    public static partial void GetTorrentTrackers(IntPtr torrentSessionHandle, out TrackerList trackers);

    /// <summary>
    /// Releases the unmanaged resources held by a <paramref name="trackers"/> list.
    /// </summary>
    /// <param name="trackers">List to release</param>
    [LibraryImport(LibraryName, EntryPoint = "destroy_torrent_tracker_list")]
    public static partial void FreeTorrentTrackerList(ref TrackerList trackers);
}
