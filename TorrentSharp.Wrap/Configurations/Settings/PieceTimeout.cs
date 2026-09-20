namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the number of seconds from a request is sent until it times out if no piece response is returned.
/// </summary>
public sealed record PieceTimeout(int Value) : ISettingsEntry<PieceTimeout>
{
    public static string Key => "piece_timeout";
    public static PieceTimeout FromValue(object value) => new((int)value);
    object ISettingsEntry<PieceTimeout>.Value => Value;
}
