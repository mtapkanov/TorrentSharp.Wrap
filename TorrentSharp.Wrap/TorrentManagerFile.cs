using TorrentSharp.Wrap.Enums;
using TorrentSharp.Wrap.Imports;

namespace TorrentSharp.Wrap;

public class TorrentManagerFile
{
    private readonly IntPtr _torrentSessionHandle;

    internal TorrentManagerFile(IntPtr torrentSessionHandle, string savePath, TorrentFileInfo info)
    {
        _torrentSessionHandle = torrentSessionHandle;

        Info = info;
        Path = System.IO.Path.IsPathRooted(Info.Path) ? Info.Path : System.IO.Path.Combine(savePath, Info.Path);
    }

    /// <summary>
    /// Static file information taken from the .torrent file.
    /// </summary>
    public TorrentFileInfo Info { get; }

    /// <summary>
    /// Where this file lives (or will live) on disk.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// This file's download priority.
    /// </summary>
    public FileDownloadPriority Priority
    {
        get => Methods.GetFilePriority(_torrentSessionHandle, Info.Index);
        set => Methods.SetFilePriority(_torrentSessionHandle, Info.Index, value);
    }
}