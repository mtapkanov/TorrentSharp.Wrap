namespace TorrentSharp.Wrap.Configurations.Settings;

/// <summary>
/// the number of seconds one block (16 kiB) is expected to be received within. If it's not, the block is requested
/// from a different peer
/// </summary>
public sealed record RequestTimeout(int Value) : ISettingsEntry<RequestTimeout>
{
    public static string Key => "request_timeout";
    public static RequestTimeout FromValue(object value) => new((int)value);
    object ISettingsEntry<RequestTimeout>.Value => Value;
}
