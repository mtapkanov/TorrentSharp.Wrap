using System.Net;

namespace TorrentSharp.Wrap.Configurations.Options;

/// <summary>
/// The IP filter, as a whole: the rules given here replace whatever was there. Where ranges overlap, the
/// later rule wins.
/// </summary>
public sealed class IpFilterOptions
{
    private readonly List<IpFilterRule> _rules = [];

    internal IpFilterOptions(TorrentClientOptionsBuilder builder) => builder.IpFilterRules = _rules;

    /// <summary>Refuses connections to and from every address from <paramref name="firstIp"/> to <paramref name="lastIp"/>, inclusive.</summary>
    public IpFilterOptions Block(string firstIp, string lastIp) => Add(firstIp, lastIp, blocked: true);

    /// <summary>Allows the range again, e.g. inside a wider blocked one.</summary>
    public IpFilterOptions Allow(string firstIp, string lastIp) => Add(firstIp, lastIp, blocked: false);

    private IpFilterOptions Add(string firstIp, string lastIp, bool blocked)
    {
        var first = IPAddress.Parse(firstIp);
        var last = IPAddress.Parse(lastIp);
        if (first.AddressFamily != last.AddressFamily)
            throw new ArgumentException($"{firstIp} and {lastIp} are not of the same address family.");

        _rules.Add(new IpFilterRule(first.ToString(), last.ToString(), blocked));
        return this;
    }
}

internal sealed record IpFilterRule(string FirstIp, string LastIp, bool Blocked);
