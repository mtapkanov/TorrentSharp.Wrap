namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>seeding_outgoing_connections</c> determines if seeding (and finished) torrents should attempt to make
/// outgoing connections or not. It may be set to false in very specific applications where the cost of making
/// outgoing connections is high, and there are no or small benefits of doing so. For instance, if no nodes are
/// behind a firewall or a NAT, seeds don't need to make outgoing connections.
/// </summary>
public sealed record SeedingOutgoingConnections(bool Value) : ISettingsEntry<SeedingOutgoingConnections>
{
    public static string Key => "seeding_outgoing_connections";
    public static SeedingOutgoingConnections FromValue(object value) => new((bool)value);
    object ISettingsEntry<SeedingOutgoingConnections>.Value => Value;
}
