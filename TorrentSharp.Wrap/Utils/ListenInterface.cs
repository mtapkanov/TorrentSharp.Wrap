using TorrentSharp.Wrap.Enums;

namespace TorrentSharp.Wrap.Utils;

/// <summary>
/// Base type for an address libtorrent should listen on for incoming connections.
/// </summary>
public abstract record ListenInterface(ListenFlags Flags)
{
    internal string FlagSuffix => Flags switch
    {
        ListenFlags.Ssl => "s",
        ListenFlags.LocalNetwork => "l",
        _ => string.Empty
    };
}