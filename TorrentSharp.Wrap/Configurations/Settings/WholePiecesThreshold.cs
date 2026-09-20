namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// if a whole piece can be downloaded in this number of seconds, or less, the peer_connection will prefer to
/// request whole pieces at a time from this peer. The benefit of this is to better utilize disk caches by doing
/// localized accesses and also to make it easier to identify bad peers if a piece fails the hash check.
/// </summary>
public sealed record WholePiecesThreshold(int Value) : ISettingsEntry<WholePiecesThreshold>
{
    public static string Key => "whole_pieces_threshold";
    public static WholePiecesThreshold FromValue(object value) => new((int)value);
    object ISettingsEntry<WholePiecesThreshold>.Value => Value;
}
