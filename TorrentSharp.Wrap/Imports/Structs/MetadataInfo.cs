using System.Runtime.InteropServices;

namespace TorrentSharp.Wrap.Imports.Structs;

/// <summary>
/// Holds the torrent-level metadata (name, creator, comment, sizes, info-hashes) parsed from a .torrent file.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal readonly struct MetadataInfo
{
    [MarshalAs(UnmanagedType.LPUTF8Str)]
    public readonly string Name;

    [MarshalAs(UnmanagedType.LPUTF8Str)]
    public readonly string Creator;

    [MarshalAs(UnmanagedType.LPUTF8Str)]
    public readonly string Comment;

    public readonly int TotalFiles;
    public readonly long TotalSize;

    public readonly long CreationEpoch;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public readonly byte[] InfoHashSha1;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public readonly byte[] InfoHashSha256;
}
