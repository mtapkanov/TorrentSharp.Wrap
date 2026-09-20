namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>unchoke_interval</c> is the number of seconds between chokes/unchokes. On this interval, peers are re-
/// evaluated for being choked/unchoked. This is defined as 30 seconds in the protocol, and it should be
/// significantly longer than what it takes for TCP to ramp up to it's max rate.
/// </summary>
public sealed record UnchokeInterval(int Value) : ISettingsEntry<UnchokeInterval>
{
    public static string Key => "unchoke_interval";
    public static UnchokeInterval FromValue(object value) => new((int)value);
    object ISettingsEntry<UnchokeInterval>.Value => Value;
}
