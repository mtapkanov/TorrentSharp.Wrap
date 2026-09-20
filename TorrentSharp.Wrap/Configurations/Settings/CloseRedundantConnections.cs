namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>close_redundant_connections</c> specifies whether libtorrent should close connections where both ends have no
/// utility in keeping the connection open. For instance if both ends have completed their downloads, there's no
/// point in keeping it open.
/// </summary>
public sealed record CloseRedundantConnections(bool Value) : ISettingsEntry<CloseRedundantConnections>
{
    public static string Key => "close_redundant_connections";
    public static CloseRedundantConnections FromValue(object value) => new((bool)value);
    object ISettingsEntry<CloseRedundantConnections>.Value => Value;
}
