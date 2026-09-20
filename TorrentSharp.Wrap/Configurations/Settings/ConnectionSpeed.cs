namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>connection_speed</c> is the number of connection attempts that are made per second. If a number &lt; 0 is
/// specified, it will default to 200 connections per second. If 0 is specified, it means don't make outgoing
/// connections at all.
/// </summary>
public sealed record ConnectionSpeed(int Value) : ISettingsEntry<ConnectionSpeed>
{
    public static string Key => "connection_speed";
    public static ConnectionSpeed FromValue(object value) => new((int)value);
    object ISettingsEntry<ConnectionSpeed>.Value => Value;
}
