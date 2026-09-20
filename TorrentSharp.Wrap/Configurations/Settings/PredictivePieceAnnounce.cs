namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// if set to &gt; 0, pieces will be announced to other peers before they are fully downloaded (and before they are
/// hash checked). The intention is to gain 1.5 potential round trip times per downloaded piece. When non-zero, this
/// indicates how many milliseconds in advance pieces should be announced, before they are expected to be completed.
/// </summary>
public sealed record PredictivePieceAnnounce(int Value) : ISettingsEntry<PredictivePieceAnnounce>
{
    public static string Key => "predictive_piece_announce";
    public static PredictivePieceAnnounce FromValue(object value) => new((int)value);
    object ISettingsEntry<PredictivePieceAnnounce>.Value => Value;
}
