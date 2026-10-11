namespace TorrentSharp.Wrap.Enums;

public enum NotificationType
{
    Generic = 0,
    TorrentStatus = 1,
    ClientPerformance = 2,
    Peer = 3,
    TorrentRemoved = 4,
    MetadataReceived = 5,
    ReadPiece = 6,
    FileRenamed = 7,
    StorageMoved = 8,
    Scrape = 9,
    ResumeData = 10,
    SessionStats = 11,
    FileError = 12,
    ListenFailed = 13,
    ListenSucceeded = 14,
    Portmap = 15,
    PortmapError = 16,
    ExternalIp = 17,
    PieceFinished = 18,
    HashFailed = 19,
    TorrentFinished = 20,
    TorrentError = 21,
    MetadataFailed = 22
}