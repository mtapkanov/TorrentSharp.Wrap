namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>use_parole_mode</c> specifies if parole mode should be used. Parole mode means that peers that participate in
/// pieces that fail the hash check are put in a mode where they are only allowed to download whole pieces. If the
/// whole piece a peer in parole mode fails the hash check, it is banned. If a peer participates in a piece that
/// passes the hash check, it is taken out of parole mode.
/// </summary>
public sealed record UseParoleMode(bool Value) : ISettingsEntry<UseParoleMode>
{
    public static string Key => "use_parole_mode";
    public static UseParoleMode FromValue(object value) => new((bool)value);
    object ISettingsEntry<UseParoleMode>.Value => Value;
}
