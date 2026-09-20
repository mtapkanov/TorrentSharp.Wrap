namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the rate based choker compares the upload rate to peers against a threshold that increases proportionally by its
/// size for every peer it visits, visiting peers in decreasing upload rate. The number of upload slots is
/// determined by the number of peers whose upload rate exceeds the threshold. This option sets the start value for
/// this threshold. A higher value leads to fewer unchoke slots, a lower value leads to more.
/// </summary>
public sealed record RateChokerInitialThreshold(int Value) : ISettingsEntry<RateChokerInitialThreshold>
{
    public static string Key => "rate_choker_initial_threshold";
    public static RateChokerInitialThreshold FromValue(object value) => new((int)value);
    object ISettingsEntry<RateChokerInitialThreshold>.Value => Value;
}
