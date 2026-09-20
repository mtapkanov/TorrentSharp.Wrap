namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// when receiving metadata (torrent file) from peers, this is the max number of bencoded tokens we're willing to
/// parse. This limit is meant to prevent DoS attacks on peers. For very large torrents, this limit may have to be
/// raised.
/// </summary>
public sealed record MetadataTokenLimit(int Value) : ISettingsEntry<MetadataTokenLimit>
{
    public static string Key => "metadata_token_limit";
    public static MetadataTokenLimit FromValue(object value) => new((int)value);
    object ISettingsEntry<MetadataTokenLimit>.Value => Value;
}
