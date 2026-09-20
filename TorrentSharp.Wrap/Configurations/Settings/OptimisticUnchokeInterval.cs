namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>optimistic_unchoke_interval</c> is the number of seconds between each *optimistic* unchoke. On this timer,
/// the currently optimistically unchoked peer will change.
/// </summary>
public sealed record OptimisticUnchokeInterval(int Value) : ISettingsEntry<OptimisticUnchokeInterval>
{
    public static string Key => "optimistic_unchoke_interval";
    public static OptimisticUnchokeInterval FromValue(object value) => new((int)value);
    object ISettingsEntry<OptimisticUnchokeInterval>.Value => Value;
}
