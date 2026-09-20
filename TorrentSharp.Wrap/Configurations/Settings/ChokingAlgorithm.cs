namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>choking_algorithm</c> specifies which algorithm to use to determine how many peers to unchoke. The unchoking
/// algorithm for downloading torrents is always "tit-for-tat", i.e. the peers we download the fastest from are
/// unchoked.
/// </summary>
public sealed record ChokingAlgorithm(int Value) : ISettingsEntry<ChokingAlgorithm>
{
    public static string Key => "choking_algorithm";
    public static ChokingAlgorithm FromValue(object value) => new((int)value);
    object ISettingsEntry<ChokingAlgorithm>.Value => Value;
}
