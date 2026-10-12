using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using TorrentSharp.Wrap.Imports;
using TorrentSharp.Wrap.Imports.Structs;

namespace TorrentSharp.Wrap;

/// <summary>
/// A single file entry belonging to a torrent.
/// </summary>
[DebuggerDisplay("{Path} ({FileSize} bytes)")]
public record TorrentFileInfo(int Index, long Offset, string Name, string Path, long FileSize, bool IsPadFile);

/// <summary>
/// Descriptive metadata parsed from a .torrent file.
/// </summary>
[DebuggerDisplay("{Name}")]
public record TorrentMetadata(string Name, string Creator, string Comment, int TotalFiles, long TotalSize, DateTimeOffset CreatedAt, string? InfoHash, string? InfoHashV2);

/// <summary>
/// A parsed .torrent file.
/// </summary>
[DebuggerDisplay("{Metadata.Name} ({Files.Count} Files)")]
public class TorrentInfo
{
    internal readonly IntPtr InfoHandle;

    /// <summary>
    /// Parses a .torrent file from the disk.
    /// </summary>
    /// <param name="fileName">Path to the .torrent file</param>
    /// <exception cref="FileNotFoundException">No file exists at <paramref name="fileName"/></exception>
    /// <exception cref="InvalidOperationException">The file couldn't be parsed</exception>
    public TorrentInfo(string fileName)
    {
        if (!File.Exists(fileName))
        {
            throw new FileNotFoundException("The specified file does not exist.", fileName);
        }

        InfoHandle = Methods.CreateTorrentFromFile(fileName);

        if (InfoHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to create torrent from file provided.");
        }
    }

    /// <summary>
    /// Parses a .torrent file already loaded into a managed byte array.
    /// </summary>
    /// <param name="fileBytes">Raw contents of a .torrent file</param>
    /// <exception cref="InvalidOperationException">The data couldn't be parsed</exception>
    public TorrentInfo(byte[] fileBytes)
    {
        InfoHandle = Methods.CreateTorrentFromBytes(fileBytes, fileBytes.Length);

        if (InfoHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to create torrent from bytes provided.");
        }
    }

    /// <summary>
    /// Parses a .torrent file from an unmanaged memory block.
    /// </summary>
    /// <param name="memoryPtr">Pointer to the first byte of the .torrent file's contents</param>
    /// <param name="length">Size of the memory block</param>
    /// <exception cref="InvalidOperationException"></exception>
    internal TorrentInfo(IntPtr memoryPtr, int length)
    {
        InfoHandle = Methods.CreateTorrentFromBytes(memoryPtr, length);

        if (InfoHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to create torrent from bytes provided.");
        }
    }

    /// <summary>
    /// Parses a .torrent file from an unmanaged memory block, given as a raw pointer.
    /// </summary>
    /// <param name="memoryPtr">Pointer to the first byte of the .torrent file's contents</param>
    /// <param name="length">Size of the memory block</param>
    /// <exception cref="InvalidOperationException"></exception>
    internal unsafe TorrentInfo(void* memoryPtr, int length)
        : this(new IntPtr(memoryPtr), length)
    {
    }

    // Принимает во владение уже выделенный lt::torrent_info*, минуя вызов create-функций.
    // Используется, когда torrent_info извлекается из хэндла после получения метаданных magnet-ссылки.
    internal TorrentInfo(IntPtr existingHandle)
    {
        InfoHandle = existingHandle;

        if (InfoHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to create torrent from handle.");
        }
    }

    // TorrentInfo часто расшаривается между потребителями, поэтому явного Dispose нет —
    // освобождение отдаём на откуп сборщику мусора
    ~TorrentInfo()
    {
        Methods.FreeTorrent(InfoHandle);
    }

    /// <summary>
    /// Metadata describing the torrent.
    /// </summary>
    public TorrentMetadata Metadata => field ??= GetInfo();

    /// <summary>
    /// The files the torrent contains.
    /// </summary>
    public IReadOnlyCollection<TorrentFileInfo> Files => field ??= GetFiles();

    /// <summary>
    /// Serializes this torrent back into .torrent file bytes.
    /// </summary>
    public byte[] GetBytes()
    {
        Methods.GetTorrentBytes(InfoHandle, out var data, out var size);

        if (data == IntPtr.Zero || size <= 0)
        {
            throw new InvalidOperationException("Failed to serialise torrent to bytes.");
        }

        try
        {
            var result = new byte[size];
            Marshal.Copy(data, result, 0, (int)size);
            return result;
        }
        finally
        {
            Methods.FreeTorrentBytes(data);
        }
    }

    /// <summary>
    /// Turns resume data saved with <see cref="Enums.ResumeDataFlags.SaveInfoDict"/> into .torrent file
    /// bytes - the usual way to keep a .torrent for a torrent added from a magnet link. The trackers and
    /// web seeds are the ones the resume data has, i.e. the torrent's current ones.
    /// </summary>
    /// <param name="resumeData">Resume data from <see cref="TorrentManager.SaveResumeDataAsync(Enums.ResumeDataFlags, CancellationToken, TimeSpan?)"/></param>
    /// <param name="torrentBytes">The .torrent file's contents, when this returns <c>true</c></param>
    /// <returns><c>false</c> if the resume data has no info dict (saved without
    /// <see cref="Enums.ResumeDataFlags.SaveInfoDict"/>, or before a magnet's metadata arrived) or can't be read</returns>
    public static bool TryCreateBytesFromResumeData(byte[] resumeData, [NotNullWhen(true)] out byte[]? torrentBytes)
    {
        torrentBytes = null;

        if (!Methods.CreateTorrentBytesFromResumeData(resumeData, resumeData.Length, out var data, out var size))
        {
            return false;
        }

        try
        {
            if (data == IntPtr.Zero || size <= 0)
            {
                return false;
            }

            torrentBytes = new byte[size];
            Marshal.Copy(data, torrentBytes, 0, (int)size);
            return true;
        }
        finally
        {
            Methods.FreeTorrentBytes(data);
        }
    }

    /// <summary>
    /// Writes this torrent to disk as a .torrent file.
    /// </summary>
    public void SaveToFile(string path)
    {
        if (!Methods.SaveTorrentToFile(InfoHandle, path))
        {
            throw new InvalidOperationException($"Failed to save torrent file to '{path}'.");
        }
    }

    private TorrentMetadata GetInfo()
    {
        var infoHandle = Methods.GetTorrentInfo(InfoHandle);

        if (infoHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to retrieve torrent metadata.");
        }

        try
        {
            var info = Marshal.PtrToStructure<MetadataInfo>(infoHandle);
            return new TorrentMetadata(info.Name,
                info.Creator,
                info.Comment,
                info.TotalFiles,
                info.TotalSize,
                DateTimeOffset.FromUnixTimeSeconds(info.CreationEpoch),
                info.InfoHashSha1.All(b => b == 0) ? null : Convert.ToHexString(info.InfoHashSha1),
                info.InfoHashSha256.All(b => b == 0) ? null : Convert.ToHexString(info.InfoHashSha256));
        }
        finally
        {
            Methods.FreeTorrentInfo(infoHandle);
        }
    }

    private IReadOnlyCollection<TorrentFileInfo> GetFiles()
    {
        Methods.GetTorrentFileList(InfoHandle, out var list);

        try
        {
            var files = new List<TorrentFileInfo>(list.Length);
            var size = Marshal.SizeOf<TorrentFile>();

            for (var i = 0; i < list.Length; i++)
            {
                var nativeFile = Marshal.PtrToStructure<TorrentFile>(list.Items + (size * i));
                var fileInfo = new TorrentFileInfo(nativeFile.Index,
                    nativeFile.Offset,
                    nativeFile.FileName,
                    nativeFile.FilePath,
                    nativeFile.FileSize,
                    nativeFile.PadFile);

                files.Add(fileInfo);
            }

            return files;
        }
        finally
        {
            Methods.FreeTorrentFileList(ref list);
        }
    }
}
