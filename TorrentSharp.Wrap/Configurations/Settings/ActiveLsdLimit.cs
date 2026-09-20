namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>active_lsd_limit</c> is the max number of torrents to announce to the local network over the local service
/// discovery protocol.
/// </summary>
public sealed record ActiveLsdLimit(int Value) : ISettingsEntry<ActiveLsdLimit>
{
    public static string Key => "active_lsd_limit";
    public static ActiveLsdLimit FromValue(object value) => new((int)value);
    object ISettingsEntry<ActiveLsdLimit>.Value => Value;
}
