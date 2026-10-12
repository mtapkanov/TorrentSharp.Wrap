namespace TorrentSharp.Wrap.Enums;

/// <summary>
/// Options for <see cref="TorrentManager.SaveResumeDataAsync(ResumeDataFlags, CancellationToken, TimeSpan?)"/>.
/// </summary>
[Flags]
public enum ResumeDataFlags
{
    None = 0,

    /// <summary>
    /// Writes cached pieces to disk before saving, so the resume data's file timestamps match what is
    /// on disk. Use it for a save that may be the last one before the process exits.
    /// </summary>
    FlushDiskCache = 1 << 0,

    /// <summary>
    /// Includes the torrent's metadata (the info dict), so the resume data alone can attach the torrent
    /// - and can be turned into a .torrent file with <see cref="TorrentInfo.TryCreateBytesFromResumeData"/>.
    /// Useful for magnet links, whose metadata otherwise has to come from peers again.
    /// </summary>
    SaveInfoDict = 1 << 1,

    /// <summary>
    /// Skips the save when nothing worth saving changed since the last one: no download progress, no
    /// settings, state or metadata change. Transfer counters don't count - a running torrent's change
    /// every second. A skipped save completes with <c>null</c>.
    /// </summary>
    IfModified = 1 << 2,
}
