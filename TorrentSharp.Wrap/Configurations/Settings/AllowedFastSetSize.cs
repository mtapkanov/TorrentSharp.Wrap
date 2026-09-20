namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the number of allowed pieces to send to peers that supports the fast extensions
/// </summary>
public sealed record AllowedFastSetSize(int Value) : ISettingsEntry<AllowedFastSetSize>
{
    public static string Key => "allowed_fast_set_size";
    public static AllowedFastSetSize FromValue(object value) => new((int)value);
    object ISettingsEntry<AllowedFastSetSize>.Value => Value;
}
