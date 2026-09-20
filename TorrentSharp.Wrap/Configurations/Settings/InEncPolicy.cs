namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// control the settings for incoming and outgoing connections respectively. see enc_policy enum for the available
/// options. Keep in mind that protocol encryption degrades performance in several respects:
/// </summary>
public sealed record InEncPolicy(int Value) : ISettingsEntry<InEncPolicy>
{
    public static string Key => "in_enc_policy";
    public static InEncPolicy FromValue(object value) => new((int)value);
    object ISettingsEntry<InEncPolicy>.Value => Value;
}
