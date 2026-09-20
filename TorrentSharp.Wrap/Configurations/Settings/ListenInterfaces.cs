namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// a comma-separated list of (IP or device name, port) pairs. These are the listen ports that will be opened for
/// accepting incoming uTP and TCP peer connections. These are also used for *outgoing* uTP and UDP tracker
/// connections and DHT nodes.
/// </summary>
public sealed record ListenInterfaces(string Value) : ISettingsEntry<ListenInterfaces>
{
    public static string Key => "listen_interfaces";
    public static ListenInterfaces FromValue(object value) => new((string)value);
    object ISettingsEntry<ListenInterfaces>.Value => Value;
}
