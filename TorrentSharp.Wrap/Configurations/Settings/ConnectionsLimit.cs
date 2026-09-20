namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>connections_limit</c> sets a global limit on the number of connections opened. The number of connections is
/// set to a hard minimum of at least two per torrent, so if you set a too low connections limit, and open too many
/// torrents, the limit will not be met.
/// </summary>
public sealed record ConnectionsLimit(int Value) : ISettingsEntry<ConnectionsLimit>
{
    public static string Key => "connections_limit";
    public static ConnectionsLimit FromValue(object value) => new((int)value);
    object ISettingsEntry<ConnectionsLimit>.Value => Value;
}
