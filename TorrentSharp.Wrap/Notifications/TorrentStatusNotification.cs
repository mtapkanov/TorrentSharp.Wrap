using TorrentSharp.Wrap.Enums;
using TorrentSharp.Wrap.Imports.Events;

namespace TorrentSharp.Wrap.Notifications;

public class TorrentStatusNotification(TorrentStatusEvent @event, TorrentManager torrentManager) : SessionNotification(@event.Info)
{
    public TorrentManager TorrentManager { get; } = torrentManager;
    public TorrentState OldState { get; } = @event.OldState;
    public TorrentState NewState { get; } = @event.NewState;
}