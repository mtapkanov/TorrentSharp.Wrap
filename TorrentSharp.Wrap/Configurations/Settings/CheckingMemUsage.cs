namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the number of blocks to keep outstanding at any given time when checking torrents. Higher numbers give faster
/// re-checks but uses more memory. Specified in number of 16 kiB blocks
/// </summary>
public sealed record CheckingMemUsage(int Value) : ISettingsEntry<CheckingMemUsage>
{
    public static string Key => "checking_mem_usage";
    public static CheckingMemUsage FromValue(object value) => new((int)value);
    object ISettingsEntry<CheckingMemUsage>.Value => Value;
}
