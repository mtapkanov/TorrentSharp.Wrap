namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// determines the encryption level of the connections. This setting will adjust which encryption scheme is offered
/// to the other peer, as well as which encryption scheme is selected by the client. See enc_level enum for options.
/// </summary>
public sealed record AllowedEncLevel(int Value) : ISettingsEntry<AllowedEncLevel>
{
    public static string Key => "allowed_enc_level";
    public static AllowedEncLevel FromValue(object value) => new((int)value);
    object ISettingsEntry<AllowedEncLevel>.Value => Value;
}
