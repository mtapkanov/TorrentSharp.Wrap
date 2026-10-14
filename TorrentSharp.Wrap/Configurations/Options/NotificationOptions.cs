using TorrentSharp.Wrap.Configurations.Settings;
using TorrentSharp.Wrap.Enums;
using TorrentSharp.Wrap.Notifications;

namespace TorrentSharp.Wrap.Configurations.Options;

/// <summary>Which notifications libtorrent generates and the client raises.</summary>
public sealed class NotificationOptions
{
    private readonly TorrentClientOptionsBuilder _builder;

    internal NotificationOptions(TorrentClientOptionsBuilder builder) => _builder = builder;

    /// <summary>
    /// The categories libtorrent generates notifications for - one outside them is never produced, and an async
    /// method waiting on one never completes. libtorrent: <c>alert_mask</c>, default <see cref="NotificationCategories.Error"/>.
    /// </summary>
    public NotificationOptions Categories(NotificationCategories categories)
    {
        _builder.Pack.Set(new AlertMask(categories));
        return this;
    }

    /// <summary>Also raises a bare <see cref="SessionNotification"/> for notification types with no dedicated class. Off by default.</summary>
    public NotificationOptions IncludeUnmapped(bool include = true)
    {
        _builder.IncludeUnmappedEvents = include;
        return this;
    }
}
