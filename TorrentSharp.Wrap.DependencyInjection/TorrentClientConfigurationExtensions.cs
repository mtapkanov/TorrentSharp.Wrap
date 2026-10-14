using System.Globalization;
using Microsoft.Extensions.Configuration;
using TorrentSharp.Wrap.Configurations;
using TorrentSharp.Wrap.Configurations.Settings;
using TorrentSharp.Wrap.Enums;
using TorrentSharp.Wrap.Utils;

namespace TorrentSharp.Wrap.DependencyInjection;

/// <summary>
/// Reads client options from configuration (e.g. a section of appsettings.json), in the same terms as the
/// fluent methods. Only the keys present are applied, at the point in the chain where this is called - so
/// put it after the values written in code that configuration may override.
/// </summary>
/// <example>
/// <code>
/// "TorrentClient": {
///   "Identity": { "UserAgent": "MyApp/1.0", "HandshakeClientVersion": "MyApp/1.0", "PeerFingerprint": "-MA0100-" },
///   "Network": { "ListenInterfaces": [ "0.0.0.0:51413", "[::]:51413" ], "IncomingTcp": true, "OutgoingTcp": true, "IncomingUtp": true, "OutgoingUtp": true },
///   "Discovery": { "Dht": true, "DhtBootstrapNodes": [ "router.bittorrent.com:6881" ], "Lsd": true, "Upnp": true, "NatPmp": true },
///   "Limits": { "Download": 0, "Upload": 0, "Connections": 200, "ActiveDownloads": 3, "ActiveSeeds": 5, "ActiveTotal": 500, "SeedingOutgoingConnections": true },
///   "Privacy": { "AnonymousMode": false, "Encryption": "Enabled" },
///   "Notifications": { "Categories": "Status, Storage, Error", "IncludeUnmapped": false },
///   "Storage": { "DefaultDownloadPath": "/data/downloads" },
///   "Timeouts": { "Default": "00:00:30" },
///   "IpFilter": { "Rules": [ { "First": "10.0.0.0", "Last": "10.255.255.255", "Blocked": true } ] }
/// }
/// </code>
/// A list given here replaces the one set before it rather than adding to it. An <c>IpFilter</c> section
/// replaces the whole filter, like <see cref="ITorrentClientOptions.IpFilter"/>.
/// </example>
public static class TorrentClientConfigurationExtensions
{
    /// <exception cref="FormatException">A value can't be read; the message names its configuration path.</exception>
    public static TOptions FromConfiguration<TOptions>(this TOptions options, IConfiguration section)
        where TOptions : ITorrentClientOptions
    {
        ArgumentNullException.ThrowIfNull(section);

        var identity = section.GetSection("Identity");
        Read(identity, "UserAgent", value => options.Identity(id => id.UserAgent(value)));
        Read(identity, "HandshakeClientVersion", value => options.Identity(id => id.HandshakeClientVersion(value)));
        Read(identity, "PeerFingerprint", value => options.Identity(id => id.PeerFingerprint(value)));

        var network = section.GetSection("Network");
        ReadList(network, "ListenInterfaces", values => options.Network(net => net.ListenOn([.. values.Select(ListenInterface.Parse)])));
        Read(network, "IncomingTcp", value => options.Advanced(pack => pack.Set(new EnableIncomingTcp(bool.Parse(value)))));
        Read(network, "OutgoingTcp", value => options.Advanced(pack => pack.Set(new EnableOutgoingTcp(bool.Parse(value)))));
        Read(network, "IncomingUtp", value => options.Advanced(pack => pack.Set(new EnableIncomingUtp(bool.Parse(value)))));
        Read(network, "OutgoingUtp", value => options.Advanced(pack => pack.Set(new EnableOutgoingUtp(bool.Parse(value)))));

        var discovery = section.GetSection("Discovery");
        Read(discovery, "Dht", value => options.Discovery(d => d.Dht(bool.Parse(value))));
        ReadList(discovery, "DhtBootstrapNodes", values => options.Discovery(d => d.DhtBootstrapNodes(values)));
        Read(discovery, "Lsd", value => options.Discovery(d => d.Lsd(bool.Parse(value))));
        Read(discovery, "Upnp", value => options.Discovery(d => d.Upnp(bool.Parse(value))));
        Read(discovery, "NatPmp", value => options.Discovery(d => d.NatPmp(bool.Parse(value))));

        var limits = section.GetSection("Limits");
        Read(limits, "Download", value => options.Limits(l => l.Download(ParseInt(value))));
        Read(limits, "Upload", value => options.Limits(l => l.Upload(ParseInt(value))));
        Read(limits, "Connections", value => options.Limits(l => l.Connections(ParseInt(value))));
        Read(limits, "ActiveDownloads", value => options.Limits(l => l.Active(downloads: ParseInt(value))));
        Read(limits, "ActiveSeeds", value => options.Limits(l => l.Active(seeds: ParseInt(value))));
        Read(limits, "ActiveTotal", value => options.Limits(l => l.Active(total: ParseInt(value))));
        Read(limits, "SeedingOutgoingConnections", value => options.Limits(l => l.SeedingOutgoingConnections(bool.Parse(value))));

        var privacy = section.GetSection("Privacy");
        Read(privacy, "AnonymousMode", value => options.Privacy(p => p.AnonymousMode(bool.Parse(value))));
        Read(privacy, "Encryption", value => options.Privacy(p => p.Encryption(Enum.Parse<EncryptionPolicy>(value, ignoreCase: true))));

        var notifications = section.GetSection("Notifications");
        Read(notifications, "Categories", value => options.Notifications(n => n.Categories(Enum.Parse<NotificationCategories>(value, ignoreCase: true))));
        Read(notifications, "IncludeUnmapped", value => options.Notifications(n => n.IncludeUnmapped(bool.Parse(value))));

        Read(section.GetSection("Storage"), "DefaultDownloadPath", value => options.Storage(s => s.DefaultDownloadPath(value)));

        Read(section.GetSection("Timeouts"), "Default", value => options.Timeouts(t => t.Default(TimeSpan.Parse(value, CultureInfo.InvariantCulture))));

        var ipFilter = section.GetSection("IpFilter");
        if (ipFilter.Exists())
        {
            var rules = ipFilter.GetSection("Rules").GetChildren().ToArray();
            options.IpFilter(filter =>
            {
                foreach (var rule in rules)
                {
                    Guard(rule.Path, () =>
                    {
                        var first = rule["First"] ?? throw new FormatException("First is missing.");
                        var last = rule["Last"] ?? first;
                        if (rule["Blocked"] is { } blocked && !bool.Parse(blocked))
                            filter.Allow(first, last);
                        else
                            filter.Block(first, last);
                    });
                }
            });
        }

        return options;
    }

    private static void Read(IConfigurationSection section, string key, Action<string> apply)
    {
        if (section[key] is { } value)
            Guard($"{section.Path}:{key}", () => apply(value));
    }

    private static void ReadList(IConfigurationSection section, string key, Action<IReadOnlyList<string>> apply)
    {
        var list = section.GetSection(key);
        if (!list.Exists())
            return;

        string[] values = [.. list.GetChildren().Select(child => child.Value).OfType<string>()];
        Guard(list.Path, () => apply(values));
    }

    private static int ParseInt(string value) => int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);

    private static void Guard(string path, Action apply)
    {
        try
        {
            apply();
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException or OverflowException)
        {
            throw new FormatException($"Invalid torrent client configuration at '{path}': {exception.Message}", exception);
        }
    }
}
