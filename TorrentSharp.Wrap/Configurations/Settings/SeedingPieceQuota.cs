namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>seeding_piece_quota</c> is the number of pieces to send to a peer, when seeding, before rotating in another
/// peer to the unchoke set.
/// </summary>
public sealed record SeedingPieceQuota(int Value) : ISettingsEntry<SeedingPieceQuota>
{
    public static string Key => "seeding_piece_quota";
    public static SeedingPieceQuota FromValue(object value) => new((int)value);
    object ISettingsEntry<SeedingPieceQuota>.Value => Value;
}
