using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap.Imports.Structs;

/// <summary>
/// One file entry from a torrent's file list.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal readonly struct TorrentFile
{
    public readonly int Index;

    public readonly long Offset;
    public readonly long FileSize;

    public readonly long ModifiedTime;

    [MarshalAs(UnmanagedType.LPUTF8Str)]
    public readonly string FileName;

    [MarshalAs(UnmanagedType.LPUTF8Str)]
    public readonly string FilePath;

    [MarshalAs(UnmanagedType.I1)]
    public readonly bool FilePathIsAbsolute;

    [MarshalAs(UnmanagedType.I1)]
    public readonly bool PadFile;
}
