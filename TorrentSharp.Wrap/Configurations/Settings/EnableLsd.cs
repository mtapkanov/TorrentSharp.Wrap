namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// Starts and stops Local Service Discovery. This service will broadcast the info-hashes of all the non-private
/// torrents on the local network to look for peers on the same swarm within multicast reach.
/// </summary>
public sealed record EnableLsd(bool Value) : ISettingsEntry<EnableLsd>
{
    public static string Key => "enable_lsd";
    public static EnableLsd FromValue(object value) => new((bool)value);
    object ISettingsEntry<EnableLsd>.Value => Value;
}
