using TorrentSharp.Wrap.Enums;

namespace TorrentSharp.Wrap;

/// <summary>
/// Connection-level information about a single peer, returned by <see cref="TorrentManager.GetPeers"/>.
/// </summary>
public sealed class ConnectedPeer
{
    public required string Address { get; init; }
    public required string Client { get; init; }

    public required long TotalDownload { get; init; }
    public required long TotalUpload { get; init; }

    public required int DownloadRate { get; init; }
    public required int UploadRate { get; init; }

    public required PeerEncryptionType EncryptionType { get; init; }
    public required PeerDirection Direction { get; init; }

    public required bool IsSeed { get; init; }
    public required bool WeAreChoking { get; init; }
    public required bool TheyAreChoking { get; init; }
    public required bool AmInterested { get; init; }
    public required bool IsInterested { get; init; }
}
