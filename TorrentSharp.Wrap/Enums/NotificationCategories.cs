namespace TorrentSharp.Wrap.Enums;

/// <summary>
/// Categories of notifications to subscribe to. Categories without a dedicated .NET struct
/// surface as a generic notification instead.
/// </summary>
/// <remarks>
/// <see cref="NotificationType"/> lists the notifications that do have a dedicated .NET struct.
/// </remarks>
[Flags]
public enum NotificationCategories
{
    None = 0,

    Error = 1 << 0,
    Peer = 1 << 1,
    PortMapping = 1 << 2,
    Storage = 1 << 3,
    Tracker = 1 << 4,
    Connect = 1 << 5,
    Status = 1 << 6,
    IPBlock = 1 << 8,
    PerformanceWarning = 1 << 9,
    DHT = 1 << 10,
    Stats = 1 << 11,
    SessionLog = 1 << 13,
    TorrentLog = 1 << 14,
    PeerLog = 1 << 15,
    IncomingRequest = 1 << 16,
    DHTLog = 1 << 17,
    DHTOperation = 1 << 18,
    PortMappingLog = 1 << 19,
    PickerLog = 1 << 20,
    FileProgress = 1 << 21,
    PieceProgress = 1 << 22,
    Upload = 1 << 23,
    BlockProgress = 1 << 24,

    All = Error |
          Peer |
          PortMapping |
          Storage |
          Tracker |
          Connect |
          Status |
          IPBlock |
          PerformanceWarning |
          DHT |
          Stats |
          SessionLog |
          TorrentLog |
          PeerLog |
          IncomingRequest |
          DHTLog |
          DHTOperation |
          PortMappingLog |
          PickerLog |
          FileProgress |
          PieceProgress |
          Upload |
          BlockProgress
}