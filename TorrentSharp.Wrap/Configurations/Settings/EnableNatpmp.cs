namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// Starts and stops the NAT-PMP service. When started, the listen port and the DHT port are attempted to be
/// forwarded on the router through NAT-PMP.
/// </summary>
public sealed record EnableNatpmp(bool Value) : ISettingsEntry<EnableNatpmp>
{
    public static string Key => "enable_natpmp";
    public static EnableNatpmp FromValue(object value) => new((bool)value);
    object ISettingsEntry<EnableNatpmp>.Value => Value;
}
