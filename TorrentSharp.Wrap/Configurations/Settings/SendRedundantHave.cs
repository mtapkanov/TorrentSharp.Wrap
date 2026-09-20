namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>send_redundant_have</c> controls if have messages will be sent to peers that already have the piece. This is
/// typically not necessary, but it might be necessary for collecting statistics in some cases.
/// </summary>
public sealed record SendRedundantHave(bool Value) : ISettingsEntry<SendRedundantHave>
{
    public static string Key => "send_redundant_have";
    public static SendRedundantHave FromValue(object value) => new((bool)value);
    object ISettingsEntry<SendRedundantHave>.Value => Value;
}
