namespace TorrentSharp.Wrap.Enums;

/// <summary>Which port-mapping protocol a <see cref="Notifications.PortmapNotification"/>/<see cref="Notifications.PortmapErrorNotification"/> refers to.</summary>
public enum PortmapTransport : byte
{
    NatPmp = 0,
    Upnp = 1
}
