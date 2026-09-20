namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>max_piece_count</c> is the maximum allowed number of pieces in metadata received via magnet links. Loading
/// large torrents (with more pieces than the default limit) may also require passing in a higher limit to
/// read_resume_data() and torrent_info::parse_info_section(), if those are used.
/// </summary>
public sealed record MaxPieceCount(int Value) : ISettingsEntry<MaxPieceCount>
{
    public static string Key => "max_piece_count";
    public static MaxPieceCount FromValue(object value) => new((int)value);
    object ISettingsEntry<MaxPieceCount>.Value => Value;
}
