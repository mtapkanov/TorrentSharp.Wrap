namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>tick_interval</c> specifies the number of milliseconds between internal ticks. This is the frequency with
/// which bandwidth quota is distributed to peers. It should not be more than one second (i.e. 1000 ms). Setting
/// this to a low value (around 100) means higher resolution bandwidth quota distribution, setting it to a higher
/// value saves CPU cycles.
/// </summary>
public sealed record TickInterval(int Value) : ISettingsEntry<TickInterval>
{
    public static string Key => "tick_interval";
    public static TickInterval FromValue(object value) => new((int)value);
    object ISettingsEntry<TickInterval>.Value => Value;
}
