namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>max_rejects</c> is the number of piece requests we will reject in a row while a peer is choked before the
/// peer is considered abusive and is disconnected.
/// </summary>
public sealed record MaxRejects(int Value) : ISettingsEntry<MaxRejects>
{
    public static string Key => "max_rejects";
    public static MaxRejects FromValue(object value) => new((int)value);
    object ISettingsEntry<MaxRejects>.Value => Value;
}
