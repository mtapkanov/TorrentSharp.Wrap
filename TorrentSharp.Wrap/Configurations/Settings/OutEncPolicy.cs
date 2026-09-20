namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// control the settings for incoming and outgoing connections respectively. see enc_policy enum for the available
/// options. Keep in mind that protocol encryption degrades performance in several respects:
/// </summary>
public sealed record OutEncPolicy(int Value) : ISettingsEntry<OutEncPolicy>
{
    public static string Key => "out_enc_policy";
    public static OutEncPolicy FromValue(object value) => new((int)value);
    object ISettingsEntry<OutEncPolicy>.Value => Value;
}
