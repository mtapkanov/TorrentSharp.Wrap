namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// always send user-agent in every web seed request. If false, only the first request per http connection will
/// include the user agent
/// </summary>
public sealed record AlwaysSendUserAgent(bool Value) : ISettingsEntry<AlwaysSendUserAgent>
{
    public static string Key => "always_send_user_agent";
    public static AlwaysSendUserAgent FromValue(object value) => new((bool)value);
    object ISettingsEntry<AlwaysSendUserAgent>.Value => Value;
}
