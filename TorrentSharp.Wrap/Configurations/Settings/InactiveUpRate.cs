namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the download and upload rate limits for a torrent to be considered active by the queuing mechanism. A torrent
/// whose download rate is less than <c>inactive_down_rate</c> and whose upload rate is less than
/// <c>inactive_up_rate</c> for <c>auto_manage_startup</c> seconds, is considered inactive, and another queued
/// torrent may be started. This logic is disabled if <c>dont_count_slow_torrents</c> is false.
/// </summary>
public sealed record InactiveUpRate(int Value) : ISettingsEntry<InactiveUpRate>
{
    public static string Key => "inactive_up_rate";
    public static InactiveUpRate FromValue(object value) => new((int)value);
    object ISettingsEntry<InactiveUpRate>.Value => Value;
}
