using TorrentSharp.Wrap.Imports.Events;

namespace TorrentSharp.Wrap.Notifications;

public class ResumeDataNotification(ResumeDataEvent @event, TorrentManager torrentManager, byte[] data) : SessionNotification(@event.Info)
{
    public TorrentManager TorrentManager { get; } = torrentManager;

    /// <summary>
    /// Whether the resume data was saved successfully.
    /// </summary>
    public bool Succeeded { get; } = @event.Succeeded;

    /// <summary>
    /// The save was requested with <see cref="Enums.ResumeDataFlags.IfModified"/> and skipped, because
    /// nothing changed since the last one. <see cref="Succeeded"/> is <c>false</c> then, but nothing failed.
    /// </summary>
    public bool NotModified { get; } = @event.NotModified;

    /// <summary>
    /// The bencoded resume data, or an empty array when <see cref="Succeeded"/> is <c>false</c>.
    /// </summary>
    public byte[] Data { get; } = data;
}
