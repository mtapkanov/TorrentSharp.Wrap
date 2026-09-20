namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// If <c>prioritize_partial_pieces</c> is true, partial pieces are picked before pieces that are more rare. If
/// false, rare pieces are always prioritized, unless the number of partial pieces is growing out of proportion.
/// </summary>
public sealed record PrioritizePartialPieces(bool Value) : ISettingsEntry<PrioritizePartialPieces>
{
    public static string Key => "prioritize_partial_pieces";
    public static PrioritizePartialPieces FromValue(object value) => new((bool)value);
    object ISettingsEntry<PrioritizePartialPieces>.Value => Value;
}
