namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// if this is true, libtorrent will fall back to listening on a port chosen by the operating system (i.e. binding
/// to port 0). If a failure is preferred, set this to false.
/// </summary>
public sealed record ListenSystemPortFallback(bool Value) : ISettingsEntry<ListenSystemPortFallback>
{
    public static string Key => "listen_system_port_fallback";
    public static ListenSystemPortFallback FromValue(object value) => new((bool)value);
    object ISettingsEntry<ListenSystemPortFallback>.Value => Value;
}
