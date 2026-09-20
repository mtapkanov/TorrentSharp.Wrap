namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>max_suggest_pieces</c> is the max number of suggested piece indices received from a peer that's remembered.
/// If a peer floods suggest messages, this limit prevents libtorrent from using too much RAM.
/// </summary>
public sealed record MaxSuggestPieces(int Value) : ISettingsEntry<MaxSuggestPieces>
{
    public static string Key => "max_suggest_pieces";
    public static MaxSuggestPieces FromValue(object value) => new((int)value);
    object ISettingsEntry<MaxSuggestPieces>.Value => Value;
}
