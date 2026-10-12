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

    /// <summary>
    /// Same as <see cref="SaveTorrentResumeData"/>, with <see cref="Enums.ResumeDataFlags"/>.
    /// </summary>
    /// <param name="torrentSessionHandle">Session-scoped torrent handle</param>
    /// <param name="flags"><see cref="Enums.ResumeDataFlags"/> as its raw value</param>
    [LibraryImport(LibraryName, EntryPoint = "save_torrent_resume_data_flags")]
    public static partial void SaveTorrentResumeDataWithFlags(IntPtr torrentSessionHandle, uint flags);

    /// <summary>
    /// Turns resume data saved with its info dict into .torrent file bytes. The buffer must be released
    /// with <see cref="FreeTorrentBytes"/>.
    /// </summary>
    /// <returns><c>false</c> if the resume data has no info dict or can't be read</returns>
    [return: MarshalAs(UnmanagedType.I1)]
    [LibraryImport(LibraryName, EntryPoint = "create_torrent_bytes_from_resume_data")]
    public static partial bool CreateTorrentBytesFromResumeData(byte[] resumeData, long resumeDataLength, out IntPtr data, out long size);
}
