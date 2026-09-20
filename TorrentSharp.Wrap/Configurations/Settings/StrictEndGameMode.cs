namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>strict_end_game_mode</c> controls when a block may be requested twice. If this is <c>true</c>, a block may
/// only be requested twice when there's at least one request to every piece that's left to download in the torrent.
/// This may slow down progress on some pieces sometimes, but it may also avoid downloading a lot of redundant
/// bytes. If this is <c>false</c>, libtorrent attempts to use each peer connection to its max, by always requesting
/// something, even if it means requesting something that has been requested from another peer already.
/// </summary>
public sealed record StrictEndGameMode(bool Value) : ISettingsEntry<StrictEndGameMode>
{
    public static string Key => "strict_end_game_mode";
    public static StrictEndGameMode FromValue(object value) => new((bool)value);
    object ISettingsEntry<StrictEndGameMode>.Value => Value;
}
