namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// when this is true, create an affinity for downloading 4 MiB extents of adjacent pieces. This is an attempt to
/// achieve better disk I/O throughput by downloading larger extents of bytes, for torrents with small piece sizes
/// </summary>
public sealed record PieceExtentAffinity(bool Value) : ISettingsEntry<PieceExtentAffinity>
{
    public static string Key => "piece_extent_affinity";
    public static PieceExtentAffinity FromValue(object value) => new((bool)value);
    object ISettingsEntry<PieceExtentAffinity>.Value => Value;
}
