namespace TorrentSharp.Wrap;


/// <summary>
/// Announce state for a single tracker, returned by <see cref="TorrentManager.GetTrackers"/>.
/// </summary>
public sealed class ConnectedTracker
{
    public required int Tier { get; init; }
    public required string Url { get; init; }

    public required bool Verified { get; init; }

    public required byte Fails { get; init; }
    public required bool Updating { get; init; }

    public required string? WarningMessage { get; init; }
    public required string? FailureMessage { get; init; }
}
