namespace TorrentSharp.Wrap.Enums;

public enum PeerNotificationType : byte
{
    ConnectedIncoming = 0,
    ConnectedOutgoing = 1,
    Disconnected = 2,
    Banned = 3,
    Snubbed = 4,
    Unsnubbed = 5,
    Errored = 6
}