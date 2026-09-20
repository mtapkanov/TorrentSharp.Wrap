namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>num_optimistic_unchoke_slots</c> is the number of optimistic unchoke slots to use. Having a higher number of
/// optimistic unchoke slots mean you will find the good peers faster but with the trade-off to use up more
/// bandwidth. 0 means automatic, where libtorrent opens up 20% of your allowed upload slots as optimistic unchoke
/// slots.
/// </summary>
public sealed record NumOptimisticUnchokeSlots(int Value) : ISettingsEntry<NumOptimisticUnchokeSlots>
{
    public static string Key => "num_optimistic_unchoke_slots";
    public static NumOptimisticUnchokeSlots FromValue(object value) => new((int)value);
    object ISettingsEntry<NumOptimisticUnchokeSlots>.Value => Value;
}
