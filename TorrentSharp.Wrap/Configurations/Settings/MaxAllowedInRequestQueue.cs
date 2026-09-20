namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the number of outstanding block requests a peer is allowed to queue up in the client. If a peer sends more
/// requests than this (before the first one has been sent) the last request will be dropped. the higher this is,
/// the faster upload speeds the client can get to a single peer.
/// </summary>
public sealed record MaxAllowedInRequestQueue(int Value) : ISettingsEntry<MaxAllowedInRequestQueue>
{
    public static string Key => "max_allowed_in_request_queue";
    public static MaxAllowedInRequestQueue FromValue(object value) => new((int)value);
    object ISettingsEntry<MaxAllowedInRequestQueue>.Value => Value;
}
