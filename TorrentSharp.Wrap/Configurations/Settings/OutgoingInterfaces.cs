namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// This controls which IP address outgoing TCP peer connections are bound to, in addition to controlling whether
/// such connections are also bound to a specific network interface/adapter (*bind-to-device*).
/// </summary>
public sealed record OutgoingInterfaces(string Value) : ISettingsEntry<OutgoingInterfaces>
{
    public static string Key => "outgoing_interfaces";
    public static OutgoingInterfaces FromValue(object value) => new((string)value);
    object ISettingsEntry<OutgoingInterfaces>.Value => Value;
}
