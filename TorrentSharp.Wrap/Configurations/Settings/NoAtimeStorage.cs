namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// <c>no_atime_storage</c> this is a Linux-only option and passes in the <c>O_NOATIME</c> to <c>open()</c> when
/// opening files. This may lead to some disk performance improvements.
/// </summary>
public sealed record NoAtimeStorage(bool Value) : ISettingsEntry<NoAtimeStorage>
{
    public static string Key => "no_atime_storage";
    public static NoAtimeStorage FromValue(object value) => new((bool)value);
    object ISettingsEntry<NoAtimeStorage>.Value => Value;
}
