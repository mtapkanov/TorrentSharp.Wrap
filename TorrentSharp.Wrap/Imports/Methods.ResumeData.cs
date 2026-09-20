using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap.Imports;

internal static partial class Methods
{
    /// <summary>
    /// Triggers an asynchronous save of a torrent's resume data. Completion is reported via a
    /// <see cref="Enums.NotificationType.ResumeData"/> notification.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    [LibraryImport(LibraryName, EntryPoint = "save_torrent_resume_data")]
    public static partial void SaveTorrentResumeData(IntPtr torrentSessionHandle);
}
