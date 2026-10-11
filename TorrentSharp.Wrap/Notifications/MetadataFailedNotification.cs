using TorrentSharp.Wrap.Imports.Events;

namespace TorrentSharp.Wrap.Notifications;

/// <summary>
/// Raised when fetching or parsing a magnet's metadata fails - the counterpart to
/// <see cref="MetadataReceivedNotification"/> for the failure case, instead of a caller having to
/// infer failure purely from its own wait timing out.
/// </summary>
public class MetadataFailedNotification(MetadataFailedEvent @event, TorrentManager torrentManager) : SessionNotification(@event.Info)
{
    public TorrentManager TorrentManager { get; } = torrentManager;

    /// <summary>The underlying error from lazy_bdecode() while parsing the received metadata.</summary>
    public int ErrorValue { get; } = @event.ErrorValue;
}
