namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>smooth_connects</c> means the number of connection attempts per second may be limited to below the
/// <c>connection_speed</c>, in case we're close to bump up against the limit of number of connections. The
/// intention of this setting is to more evenly distribute our connection attempts over time, instead of attempting
/// to connect in batches, and timing them out in batches.
/// </summary>
public sealed record SmoothConnects(bool Value) : ISettingsEntry<SmoothConnects>
{
    public static string Key => "smooth_connects";
    public static SmoothConnects FromValue(object value) => new((bool)value);
    object ISettingsEntry<SmoothConnects>.Value => Value;
}
