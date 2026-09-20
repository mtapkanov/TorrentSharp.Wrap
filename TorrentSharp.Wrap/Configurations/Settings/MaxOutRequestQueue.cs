namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>max_out_request_queue</c> is the maximum number of outstanding requests to send to a peer. This limit takes
/// precedence over <c>request_queue_time</c>. i.e. no matter the download speed, the number of outstanding requests
/// will never exceed this limit.
/// </summary>
public sealed record MaxOutRequestQueue(int Value) : ISettingsEntry<MaxOutRequestQueue>
{
    public static string Key => "max_out_request_queue";
    public static MaxOutRequestQueue FromValue(object value) => new((int)value);
    object ISettingsEntry<MaxOutRequestQueue>.Value => Value;
}
