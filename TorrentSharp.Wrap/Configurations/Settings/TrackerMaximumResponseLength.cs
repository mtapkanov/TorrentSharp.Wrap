namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// this is the maximum number of bytes in a tracker response. If a response size passes this number of bytes it
/// will be rejected and the connection will be closed. On gzipped responses this size is measured on the
/// uncompressed data. So, if you get 20 bytes of gzip response that'll expand to 2 megabytes, it will be
/// interrupted before the entire response has been uncompressed (assuming the limit is lower than 2 MiB).
/// </summary>
public sealed record TrackerMaximumResponseLength(int Value) : ISettingsEntry<TrackerMaximumResponseLength>
{
    public static string Key => "tracker_maximum_response_length";
    public static TrackerMaximumResponseLength FromValue(object value) => new((int)value);
    object ISettingsEntry<TrackerMaximumResponseLength>.Value => Value;
}
