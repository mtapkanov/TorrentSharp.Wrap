using System.Net;
using TorrentSharp.Wrap.Enums;

namespace TorrentSharp.Wrap.Utils;

/// <summary>
/// A listen interface identified by an IP address and port.
/// </summary>
public sealed record IpInterface(IPEndPoint Endpoint, ListenFlags Flags = ListenFlags.None) : ListenInterface(Flags)
{
    public override string ToString() => $"{Endpoint}{FlagSuffix}";
}