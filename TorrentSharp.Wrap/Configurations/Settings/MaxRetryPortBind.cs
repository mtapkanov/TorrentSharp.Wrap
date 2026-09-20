namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// if binding to a specific port fails, should the port be incremented by one and tried again? This setting
/// specifies how many times to retry a failed port bind
/// </summary>
public sealed record MaxRetryPortBind(int Value) : ISettingsEntry<MaxRetryPortBind>
{
    public static string Key => "max_retry_port_bind";
    public static MaxRetryPortBind FromValue(object value) => new((int)value);
    object ISettingsEntry<MaxRetryPortBind>.Value => Value;
}
