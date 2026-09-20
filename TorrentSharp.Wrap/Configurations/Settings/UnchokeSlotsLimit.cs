namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>unchoke_slots_limit</c> is the max number of unchoked peers in the session. The number of unchoke slots may
/// be ignored depending on what <c>choking_algorithm</c> is set to. Setting this limit to -1 means unlimited, i.e.
/// all peers will always be unchoked.
/// </summary>
public sealed record UnchokeSlotsLimit(int Value) : ISettingsEntry<UnchokeSlotsLimit>
{
    public static string Key => "unchoke_slots_limit";
    public static UnchokeSlotsLimit FromValue(object value) => new((int)value);
    object ISettingsEntry<UnchokeSlotsLimit>.Value => Value;
}
