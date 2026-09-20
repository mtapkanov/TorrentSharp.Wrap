namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// sets the i2p_ SAM bridge to connect to. set the port with the <c>i2p_port</c> setting. Unless this is set, i2p
/// torrents are not supported. This setting is separate from the other proxy settings since i2p torrents and their
/// peers are orthogonal. You can have i2p peers as well as regular peers via a proxy.
/// </summary>
public sealed record I2pHostname(string Value) : ISettingsEntry<I2pHostname>
{
    public static string Key => "i2p_hostname";
    public static I2pHostname FromValue(object value) => new((string)value);
    object ISettingsEntry<I2pHostname>.Value => Value;
}
