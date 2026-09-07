using TorrentSharp.Wrap.Enums;

namespace TorrentSharp.Wrap.Utils;

/// <summary>
/// A listen interface identified by network adapter GUID (Windows only).
/// </summary>
public sealed record GuidInterface(Guid Guid, ListenFlags Flags = ListenFlags.None) : ListenInterface(Flags)
{
    public override string ToString() => $"{Guid.ToString("B").ToUpperInvariant()}{FlagSuffix}";
}
