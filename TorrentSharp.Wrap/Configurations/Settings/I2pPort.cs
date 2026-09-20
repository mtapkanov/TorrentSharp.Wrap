namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// sets the i2p_ SAM bridge port to connect to. set the hostname with the <c>i2p_hostname</c> setting.
/// </summary>
public sealed record I2pPort(int Value) : ISettingsEntry<I2pPort>
{
    public static string Key => "i2p_port";
    public static I2pPort FromValue(object value) => new((int)value);
    object ISettingsEntry<I2pPort>.Value => Value;
}
