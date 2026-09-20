namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>prefer_udp_trackers</c>: true means that trackers may be rearranged in a way that udp trackers are always
/// tried before http trackers for the same hostname. Setting this to false means that the tracker's tier is
/// respected and there's no preference of one protocol over another.
/// </summary>
public sealed record PreferUdpTrackers(bool Value) : ISettingsEntry<PreferUdpTrackers>
{
    public static string Key => "prefer_udp_trackers";
    public static PreferUdpTrackers FromValue(object value) => new((bool)value);
    object ISettingsEntry<PreferUdpTrackers>.Value => Value;
}
