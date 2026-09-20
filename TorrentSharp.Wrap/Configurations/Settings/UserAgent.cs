namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// this is the client identification to the tracker. The recommended format of this string is: "client-name/client-
/// version libtorrent/libtorrent-version". This name will not only be used when making HTTP requests, but also when
/// sending extended headers to peers that support that extension. It may not contain \r or \n
/// </summary>
public sealed record UserAgent(string Value) : ISettingsEntry<UserAgent>
{
    public static string Key => "user_agent";
    public static UserAgent FromValue(object value) => new((string)value);
    object ISettingsEntry<UserAgent>.Value => Value;
}
