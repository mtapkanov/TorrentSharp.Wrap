using System.Net;
using TorrentSharp.Wrap.Enums;

namespace TorrentSharp.Wrap.Utils;

public abstract record ListenInterface(ListenFlags Flags)
{
    internal string FlagSuffix => Flags switch
    {
        ListenFlags.Ssl => "s",
        ListenFlags.LocalNetwork => "l",
        _ => string.Empty
    };

    /// <summary>
    /// Reads one interface in libtorrent's own notation - the same one <see cref="object.ToString"/> writes:
    /// <c>0.0.0.0:6881</c>, <c>[::]:6881</c> or <c>{GUID}</c>, each optionally followed by <c>s</c> (SSL) or
    /// <c>l</c> (local network only).
    /// </summary>
    /// <exception cref="FormatException">Not an address with a port, nor a braced GUID</exception>
    public static ListenInterface Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var text = value.Trim();
        var flags = ListenFlags.None;
        if (text.EndsWith('s'))
        {
            flags = ListenFlags.Ssl;
            text = text[..^1];
        }
        else if (text.EndsWith('l'))
        {
            flags = ListenFlags.LocalNetwork;
            text = text[..^1];
        }

        if (text.StartsWith('{') && Guid.TryParse(text, out var guid))
            return new GuidInterface(guid, flags);

        // IPEndPoint.TryParse also takes a bare address (as port 0) and an unbracketed IPv6 one - libtorrent
        // takes neither, so require the port and, for IPv6, the brackets.
        var portSeparator = text.LastIndexOf(':');
        var bracketedOrIPv4 = text.StartsWith('[') || text.IndexOf(':') == portSeparator;
        if (portSeparator > text.LastIndexOf(']') && bracketedOrIPv4 && IPEndPoint.TryParse(text, out var endpoint))
            return new IpInterface(endpoint, flags);

        throw new FormatException($"'{value}' is not a listen interface: expected address:port, [address]:port or {{GUID}}.");
    }
}
